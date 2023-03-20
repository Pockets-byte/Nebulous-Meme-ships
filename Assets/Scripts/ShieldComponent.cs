using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using UnityEngine;
using UnityEngine.VFX;

using Munitions;
using Ships;

namespace Geoscream
{
    public class ShieldComponent : CycledComponent
    {
        private float? _lastVfxTime;
        private float _damageTaken;
        [Header("Shield Gen")]
        [Tooltip("Optional subtype name for the burst duration, cooldown time, and battleshort damage prob stats.")]
        [SerializeField]
        protected string _statGroupSubtype;
        [SerializeField]
        protected ContinuousWeaponComponent.CooldownType _cooldownStyle;
        [SerializeField]
        protected VisualEffect shieldHitVfx;
        [Tooltip("If checked, the shield hit VFX will be rotated as if the impacting shell ricocheted. Unchecked, the VFX will be faced perpendicular to the incoming shell.")]
        [SerializeField]
        protected bool _ricochetEffect = true;
        [Tooltip("The distance away from the ship where the VFX will spawn (in Nebulous units/decimeters).")]
        [SerializeField]
        protected float _vfxDistance = 0.25f;
        [Tooltip("When true, this component will operate continuously and ignore its cooldown until it destroys itself.")]
        [SerializeField]
        protected bool _battleShortIgnoreCooldown;
        [Tooltip("When true, damage is dealt to the module's HP AND the shield capacity.")]
        [SerializeField]
        protected bool _damageDealtToSelf;
        [SerializeField]
        protected float _maxShieldCapacity = 7200f;
        [SerializeField]
        protected float _cooldownTime = 300f;
        [Tooltip("Using the default implementation, the time to go from 0% to almost full (Eve Online shield formula). Set to float.PositiveInfinity to disable")]
        [SerializeField]
        protected float _rechargeTime = 300f;
        [Tooltip("Capacity loss per second. Set to 0 to disable in the default implementation.")]
        [SerializeField]
        protected float _decayRate;
        [Tooltip("When below this fraction of shield capacity, damage will leak through to the armor. Set to 0 to ignore")]
        [SerializeField]
        protected float _shieldLeakFraction = 0.2f;
        [ShipStat("shieldgenerator-cooldown", "Recycle Time", "s", InitializeFrom = "_cooldownTime", LimitSubtypeModifiersOnly = true, MinValue = 1f, NameSubtypeFrom = "_statGroupSubtype", PositiveBad = true, StackingPenalty = true)]
        protected StatValue _statCooldownTime;
        [ShipStat("shieldgenerator-shieldcapacity", "Shield Capacity", "", InitializeFrom = "_maxShieldCapacity", LimitSubtypeModifiersOnly = true, MinValue = 1f, NameSubtypeFrom = "_statGroupSubtype", StackingPenalty = true)]
        protected StatValue _statShieldCapacity;
        [ShipStat("shieldgenerator-rechargetime", "Recharge Time", "s", InitializeFrom = "_rechargeTime", LimitSubtypeModifiersOnly = true, MinValue = 1f, NameSubtypeFrom = "_statGroupSubtype", PositiveBad = true, StackingPenalty = true)]
        protected StatValue _statRechargeTime;
        [ShipStat("shieldgenerator-decaytime", "Decay Rate", "/s", InitializeFrom = "_decayRate", LimitSubtypeModifiersOnly = true, MinValue = 0.0f, NameSubtypeFrom = "_statGroupSubtype", PositiveBad = true, StackingPenalty = true)]
        protected StatValue _statDecayRate;

        public override bool HasCycleTimer => (double)this._statShieldCapacity.Value != 0.0 && this._cooldownStyle > ContinuousWeaponComponent.CooldownType.None;

        protected override float _cycleLength => this._statCooldownTime.Value;

        public override float BurstPercent => (double)this._statShieldCapacity.Value == 0.0 ? 0.0f : Mathf.Clamp(this._damageTaken / this._statShieldCapacity.Value, 0.0f, 1f);

        public IDamageDealer CurrentDamageDealer { get; set; }

        public MunitionHitInfo CurrentHitInfo { get; set; }

        protected override void Awake() => base.Awake();

        protected override void Start()
        {
            base.Start();
            this._damageTaken = 0.0f;
        }

        protected override ComponentActivity GetFunctionalActivityStatus()
        {
            if (this.HasCycleTimer && this.CycleActive)
                return ComponentActivity.Cycling;
            return (double)this._damageTaken <= 0.0 ? ComponentActivity.Active : ComponentActivity.ActiveTimed;
        }

        protected override void Update()
        {
            if (this.GetFunctionalActivityStatus() <= ComponentActivity.ActiveTimed)
            {
                if ((double)this._damageTaken > (double)this._statShieldCapacity.Value)
                {
                    if (this.HasCycleTimer)
                    {
                        this._cycleRpcProvider.RpcMarkCycle(this.RpcKey, 0.0f);
                        this._damageTaken = 0.0f;
                    }
                    else
                        this._damageTaken = this._statShieldCapacity.Value;
                }
                else
                {
                    if (ShieldComponent.func_GetRecharge == null)
                        throw new NullReferenceException("WHY IS THIS NULL? -M1");
                    this._damageTaken = !ShieldComponent.func_GetRecharge.ContainsKey(this._partKey) ? Mathf.Max(this._damageTaken - this.GetRecharge(Time.deltaTime), 0.0f) : Mathf.Max(this._damageTaken - ShieldComponent.func_GetRecharge[this._partKey](Time.deltaTime), 0.0f);
                }
            }
            base.Update();
        }

        public override string GetFormattedStats(bool full, IEnumerable<IHullComponent> group) => this.GetFormattedStats(full, group.Count<IHullComponent>());

        public override string GetFormattedStats(bool full, int groupSize = 1)
        {
            StringBuilder stringBuilder = new StringBuilder(base.GetFormattedStats(full, groupSize));
            if ((double)this._statShieldCapacity.Value < double.PositiveInfinity)
                stringBuilder.AppendLine(this._statShieldCapacity.FullTextWithLink);
            if (this._cooldownStyle > ContinuousWeaponComponent.CooldownType.None)
                stringBuilder.AppendLine(this._statCooldownTime.FullTextWithLink);
            if ((double)this._statRechargeTime.Value < double.PositiveInfinity)
                stringBuilder.AppendLine(this._statRechargeTime.FullTextWithLink);
            if ((double)this._statDecayRate.Value > 0.0)
                stringBuilder.AppendLine(this._statDecayRate.FullTextWithLink);
            return stringBuilder.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetRecharge(float dt)
        {
            float num = this._statShieldCapacity.Value * (1f - this.BurstPercent);
            return dt * (float)(10.0 * (double)this._statShieldCapacity.Value * ((double)Mathf.Sqrt(num / this._statShieldCapacity.Value) - (double)num / (double)this._statShieldCapacity.Value) / (double)this._statRechargeTime.Value + 0.10000000149011612) - this._statDecayRate.Value;
        }

        public void TakeDamage()
        {
            if (this.CurrentDamageDealer == null || this.CurrentHitInfo == null)
            {
                Debug.LogError((object)"CurrentDamageDealer or CurrentHitInfo is null! -M1 Shield Generator");
            }
            else
            {
                this._lastVfxTime = new float?(Time.time);
                VisualEffect visualEffect = UnityEngine.Object.Instantiate<VisualEffect>(this.shieldHitVfx, this.CurrentHitInfo.Point - this._vfxDistance * this.CurrentHitInfo.HitNormal, this._ricochetEffect ? Quaternion.LookRotation(this.CurrentHitInfo.HitNormal - 2f * Vector3.Dot(this.CurrentHitInfo.HitNormal, this.CurrentHitInfo.Normal) * this.CurrentHitInfo.Normal) : Quaternion.LookRotation(this.CurrentHitInfo.Normal));
                if (this.shieldHitVfx.HasFloat("BurstPercent"))
                    visualEffect.SetFloat("BurstPercent", this.BurstPercent);
                visualEffect.Play();
                UnityEngine.Object.Destroy((UnityEngine.Object)visualEffect, 5f);
                string partKey = this._partKey;
                if (ShieldComponent.func_TakeDamage.ContainsKey(partKey))
                {
                    float damageDone = ShieldComponent.func_TakeDamage[partKey](this.CurrentHitInfo, this.CurrentDamageDealer);
                    this._damageTaken += damageDone;
                    if (!this._damageDealtToSelf)
                        return;
                    this.DoDamageToSelf(damageDone);
                }
                else if (!this._damageDealtToSelf)
                    this._damageTaken += Mathf.Pow(this.CurrentDamageDealer.ArmorPenetration, 0.25f) * Mathf.Pow(this.CurrentDamageDealer.ComponentDamage, 0.75f);
                else
                    this.DoDamageToSelf((float)(1.6666667461395264 * (1.0 - (double)this._myHull.ComponentDR)));
            }
        }

        public float ShieldLeakFraction => this._shieldLeakFraction;

        public static Dictionary<string, Func<MunitionHitInfo, IDamageDealer, float>> func_TakeDamage { get; set; } = new Dictionary<string, Func<MunitionHitInfo, IDamageDealer, float>>();

        public static Dictionary<string, Func<float, float>> func_GetRecharge { get; set; } = new Dictionary<string, Func<float, float>>();
    }
}
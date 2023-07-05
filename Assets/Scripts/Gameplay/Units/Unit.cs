﻿using System;
using ScriptableObjects.Sound;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Units
{
    
    public enum UnitState
    {
        Default,
        Reloading,
    }

    public enum EngineStatus
    {
        Idle,
        Walking,
        Running,
    }
    [RequireComponent(typeof(Rigidbody))]
    public class Unit : MonoBehaviour, IHealth
    {
        private Module[] _modules;

        public UnityEvent<float, float> onHealthChange;

        private Rigidbody _rb;
        
        public UnityEvent<Unit> onUnitDeath;
        
        [Header("Sounds")]
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private EngineAudioSO engineAudioSo;
        
           public virtual float Health { get; set; } = 100;
           public float MaxHealth { get; set; } = 100;
        [SerializeField] private Faction faction;
        public Faction Faction { get => faction; set => faction = value; }
        public virtual UnitState State { get; set; } = UnitState.Default;
        
        private EngineStatus _engineStatus = EngineStatus.Idle;

        protected virtual EngineStatus EngineStatus
        {
            get => _engineStatus;
            set
            {
                if (value == _engineStatus) return;
                engineAudioSource.clip = value switch
                {
                    EngineStatus.Idle => engineAudioSo.engineIdle,
                    EngineStatus.Walking => engineAudioSo.engineWalking,
                    EngineStatus.Running => engineAudioSo.engineSpeeding,
                    _ => throw new ArgumentOutOfRangeException()
                };
                engineAudioSource.PlayOneShot((int)_engineStatus < (int)value
                    ? engineAudioSo.engineAcceleration
                    : engineAudioSo.engineDeceleration, 0.5f);

                engineAudioSource.loop = true;
                engineAudioSource.Play();
                
                _engineStatus = value;
            }
        }

        public virtual float Priority => 5;

        protected virtual void Start()
        {
            _modules = GetComponentsInChildren<Module>();
            foreach (var module in _modules)
                module.faction = Faction;
            onHealthChange.Invoke(Health, MaxHealth); // Initialize health bar
            
            _rb = GetComponent<Rigidbody>();
            engineAudioSource.clip = engineAudioSo.engineIdle;
            engineAudioSource.loop = true;
            engineAudioSource.Play();
        }

        protected virtual void OnEnable()
        {
            EventManager.AddListener("OnPause", OnPause);
            EventManager.AddListener("OnResume", OnResume);
        }
        
        protected virtual void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPause);
            EventManager.RemoveListener("OnResume", OnResume);
        }

        protected virtual void OnPause()
        {
            engineAudioSource.Pause();
            Debug.Log("Pause Unit" + engineAudioSource.isPlaying);
        }
        
        protected virtual void OnResume()
        {
            engineAudioSource.UnPause();
        }

     
        public virtual void OnTakeDamage(DamagePackage damagePackage)
        {
            onHealthChange?.Invoke(Health, MaxHealth);
        }

        protected virtual void Update()
        {
            Debug.Log("Velocity of " + name + " is " + _rb.velocity.magnitude + " and engine status is " + EngineStatus);
            EngineStatus = _rb.velocity.magnitude switch
            {
                < 5 => EngineStatus.Idle,
                < 40 => EngineStatus.Walking,
                _ => EngineStatus.Running
            };
        }


        public virtual void Die()
        {
            Destroy(gameObject);
            onUnitDeath.Invoke(this);
            Factions.RemoveMember(faction, this);
        }

        public virtual void Awake()
        {
            Factions.AddMember(faction, this);
        }
    }
}
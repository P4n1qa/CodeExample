using System;
using System.Collections.Generic;
using System.Text;
using AI.Npc;
using AI.Npc.AIBrain;
using AI.Npc.Animation;
using AI.Npc.BodySystem;
using AI.Npc.CharacterData;
using AI.Npc.MoveSystem;
using AI.Npc.WeaponSystem;
using UnityEngine;

namespace AI.Marshals
{
    public class NpcController : MonoBehaviour ,INpcController
    {
        public bool IsReady;
        
        public event Action<string> OnReady;
        public Root Root { get; private set; }
        public IAttackSystem AttackSystem { get; private set;}
        public IMoveSystem MoveSystem { get; private set;}
        public CharacterData CharacterData { get; private set;}
        public IAIBrain AIBrain { get; private set;}
        
        public IBodySystem BodySystem { get; private set;}
        public IAnimationController AnimationController{ get; private set;}
        
        private readonly List<ISystem> _systems = new();
        private readonly List<string> _namesOfReadyControllers = new();
        
        private int _systemsCounter;
        
        public void Initialize(Root root)
        {
            Root = root;
            _namesOfReadyControllers.Clear();
            InitializeSystems();
            Invoke(nameof(OnSystemPreparationWasInterrupted),10f);
        }

        private void InitializeSystems()
        {
            AttackSystem = GetComponent<IAttackSystem>();
            MoveSystem = GetComponent<IMoveSystem>();
            CharacterData = GetComponent<CharacterData>();
            AIBrain = GetComponent<IAIBrain>();
            AnimationController = GetComponent<IAnimationController>();
            BodySystem = GetComponent<IBodySystem>();
            
            _systems.Add(AnimationController as ISystem);
            _systems.Add(AttackSystem as ISystem);
            _systems.Add(CharacterData);
            _systems.Add(AIBrain as ISystem);
            _systems.Add(MoveSystem as ISystem);
            _systems.Add(BodySystem as ISystem);

            _systemsCounter = _systems.Count;
            
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].OnReady += SystemReadyHandler;
                _systems[i].Initialize(this);
            }
        }

        private void OnSystemPreparationWasInterrupted()
        {
            Unsubscribe();
            IsReady = false;
            StringBuilder message = new StringBuilder();
            message.Append("ERROR Time left for preparation system! Successfully prepared : ");
            
            for (int i = 0; i < _namesOfReadyControllers.Count; i++)
            {
                message.Append($"/ {_namesOfReadyControllers[i]}");
            }
            
            OnReady?.Invoke($"{message}");
        }
        
        private void SystemReadyHandler(string errorMessage, string controllerName)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                _namesOfReadyControllers.Add(controllerName);
                _systemsCounter --;
                
                if (_systemsCounter != 0) return;
                
                CancelInvoke(nameof(OnSystemPreparationWasInterrupted));
                Debug.Log("Npc: All Systems Ready!");
                Unsubscribe();
                IsReady = true;
                OnReady?.Invoke(string.Empty);
            }
            else
            {
                CancelInvoke(nameof(OnSystemPreparationWasInterrupted));
                Unsubscribe();
                IsReady = false;
                OnReady?.Invoke($"Npc Controller nor ready. ErrorMessage = {errorMessage}");
                Debug.LogError(errorMessage);
            }
        }

        private void Unsubscribe()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].OnReady -= SystemReadyHandler;
            }
        }
    }
}
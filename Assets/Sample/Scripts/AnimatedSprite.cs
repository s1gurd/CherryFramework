using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.TickDispatcher;
using GeneratedDataModels;
using UnityEngine;

namespace Sample
{
    // Behaviours inherited from BehaviourBase manage their subscriptions to different services automatically
    // No need to unsubscribe in OnDestroy
    
    // To use Ticker Service instead of ordinary Update(), a class must derive from ITickable, IFixedTickable
    // or ILateTickable interfaces in any combination
    // To start receiving ticks just call (Ticker instance).Register(this, (optional tick period)). To stop - call Unregister
    public class AnimatedSprite : BehaviourBase, ITickable
    {
        [SerializeField] public Sprite[] sprites;
    
        [Inject] private readonly ModelService _modelService;
        [Inject] private readonly Ticker _ticker;
    
        private SpriteRenderer _spriteRenderer;
        private int _currentFrame;
    
        void Start()
        { 
            _spriteRenderer = GetComponent<SpriteRenderer>();
            //We want sprites to be animated even when the game is paused and stop the animation only
            //when the player becomes dead
            var gameState = _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            Bindings.CreateBinding(gameState.ReadyAccessor, ready =>
            {
                if (!ready) return;
                Bindings.CreateBinding(gameState.PlayerDeadAccessor, dead =>
                {
                    if (!dead)
                    {
                        _ticker.Register(this, 1 / gameState.GameSpeed);
                    }
                    else
                    {
                        _ticker.UnRegister(this);
                    }
                });
            
                //Adjust the animation according to game speed
                Bindings.CreateBinding(gameState.GameSpeedAccessor, speed =>
                {
                    _ticker.UnRegister(this);
                    _ticker.Register(this, 1 / gameState.GameSpeed);
                });
            });
        }

        void Animate(){
            _currentFrame++;
            if(_currentFrame>=sprites.Length){
                _currentFrame=0;
            }
            _spriteRenderer.sprite=sprites[_currentFrame];

            //Invoke(nameof(Animate), 1f/GameManager.Instance.gameSpeed);
        }

        public void Tick(float deltaTime)
        {
            _currentFrame++;
            _currentFrame %= sprites.Length;
            _spriteRenderer.sprite = sprites[_currentFrame];
        }
    }
}

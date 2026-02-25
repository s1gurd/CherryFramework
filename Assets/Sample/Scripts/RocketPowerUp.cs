using CherryFramework.DependencyManager;
using DG.Tweening;
using Sample.Settings;
using UnityEngine;

namespace Sample
{
    public class RocketPowerUp : Obstacle
    {
        [Inject] private readonly GameSettings _gameSettings;
        
        protected override void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !StateService.IsStatusActive(EventKeys.RocketPowerUp))
            {
                StateService.SetStatus(EventKeys.RocketPowerUp);
                
                // Here we add a processor to a field in model. Processor alters field value when it is read.
                // There may be many processor on a single field.
                // Processor may have a priority of calling. Lesser are called earlier.
                // Processors with equal priorities are called in order of adding
                // To get processed value, use model.fieldAccessor.ProcessedValue
                // To get original value, use model.field
                var processor = GameState.JumpForceAccessor.AddProcessor(f => f * _gameSettings.jumpForceMultiplier);
                var seq = DOTween.Sequence();
                seq.AppendInterval(_gameSettings.powerUpLifetime);
                seq.AppendCallback(() =>
                {
                    StateService.UnsetStatus(EventKeys.RocketPowerUp);
                    // Remove processor from list
                    GameState.JumpForceAccessor.RemoveProcessor(processor);
                });
                gameObject.SetActive(false);
            }
        }
    }
}
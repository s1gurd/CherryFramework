using UnityEngine;

namespace CherryFramework.TickDispatcher
{
    public class TickerBehaviour : MonoBehaviour
    {
        private Ticker _ticker;

        internal void Setup(Ticker ticker)
        {
            _ticker = ticker;
        }

        private void Update()
        {
            _ticker.Update();
        }

        private void LateUpdate()
        {
            _ticker.LateUpdate();
        }

        private void FixedUpdate()
        {
            _ticker.FixedUpdate();
        }
    }
}
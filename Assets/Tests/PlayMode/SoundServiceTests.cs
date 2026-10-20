using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CherryFramework.DependencyManager;
using CherryFramework.SoundService;
using CherryFramework.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoundServiceType = CherryFramework.SoundService.SoundService;

namespace CherryFramework.PlayMode.Tests
{
    public class SoundServiceTests
    {
        private const string EventKey = "test_event";

        private GlobalAudioSettings _settings;
        private AudioEventsCollection _collection;
        private GameObject _emitterObject;
        private GameObject _cameraObject;
        private SoundServiceType _soundService;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GlobalAudioSettings>();
            _collection = ScriptableObject.CreateInstance<AudioEventsCollection>();
            _collection.audioEvents = new List<AudioEvent>
            {
                new AudioEvent { eventKey = EventKey, volume = 1f }
            };

            // The emitter injects the global settings and the listener camera,
            // so it stays inactive until the sound service has bound them.
            _emitterObject = new GameObject("TestEmitter");
            _emitterObject.SetActive(false);
            var emitter = _emitterObject.AddComponent<AudioEmitter>();
            var source = _emitterObject.AddComponent<AudioSource>();
            SerializedFields.SetField(emitter, "source", source);
            _settings.emitterSample = emitter;

            _cameraObject = new GameObject("TestCamera");
            var camera = _cameraObject.AddComponent<Camera>();

            _soundService = new SoundServiceType(_settings, _collection, camera);

            _emitterObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            // A test may have disposed the service already, and the service
            // logs an error when a binding is removed twice.
            if (DependencyContainer.Instance.HasDependency<GlobalAudioSettings>())
            {
                _soundService.Dispose();
            }

            Object.DestroyImmediate(_emitterObject);
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_collection);
        }

        [UnityTest]
        public IEnumerator Play_OfAnUnknownEvent_LogsAnErrorAndReturnsZero()
        {
            LogAssert.Expect(LogType.Error, new Regex("No sound event with the name"));

            var handler = _soundService.Play("there_is_no_such_event", _emitterObject.transform);

            Assert.AreEqual(0, handler);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Play_OfAKnownEvent_ReturnsAHandler()
        {
            var handler = _soundService.Play(EventKey, _emitterObject.transform);

            Assert.AreNotEqual(0, handler);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Play_OfAKnownEvent_FindsTheEmitterByTheHandler()
        {
            var handler = _soundService.Play(EventKey, _emitterObject.transform);

            var emitter = _soundService.GetEmitter(handler);

            Assert.IsNotNull(emitter);
            Assert.AreEqual(EventKey, emitter.EventKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Play_OfAKnownEvent_FindsTheEmitterByTheEventKey()
        {
            _soundService.Play(EventKey, _emitterObject.transform);

            var emitters = _soundService.GetEmitters(EventKey);

            Assert.AreEqual(1, emitters.Count());
            yield return null;
        }

        [UnityTest]
        public IEnumerator Play_Twice_ReturnsDifferentHandlers()
        {
            var first = _soundService.Play(EventKey, _emitterObject.transform);
            var second = _soundService.Play(EventKey, _emitterObject.transform);

            Assert.AreNotEqual(first, second);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Stop_OfAnUnknownHandler_LogsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("No emitter with handler"));

            _soundService.Stop(999);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Stop_OfAKnownHandler_DoesNotThrow()
        {
            var handler = _soundService.Play(EventKey, _emitterObject.transform);

            Assert.DoesNotThrow(() => _soundService.Stop(handler));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FadeOut_OfAnUnknownHandler_LogsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("No emitter with handler"));

            _soundService.FadeOut(999, 0.1f);

            yield return null;
        }

        [UnityTest]
        public IEnumerator FadeOut_OfAKnownHandler_DoesNotThrow()
        {
            var handler = _soundService.Play(EventKey, _emitterObject.transform);

            Assert.DoesNotThrow(() => _soundService.FadeOut(handler, 0.1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StopAll_LeavesNoActiveEmitters()
        {
            _soundService.Play(EventKey, _emitterObject.transform);

            _soundService.StopAll();

            Assert.AreEqual(0, _soundService.GetEmitters(EventKey).Count());
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheConstructor_BindsTheAudioSettingsAndTheListenerCamera()
        {
            Assert.IsTrue(DependencyContainer.Instance.HasDependency<GlobalAudioSettings>());

            var listenerCamera = typeof(SoundServiceType).Assembly.GetType("CherryFramework.SoundService.ListenerCamera");
            Assert.IsNotNull(listenerCamera);
            Assert.IsTrue(DependencyContainer.Instance.HasDependency(listenerCamera));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Dispose_RemovesTheBindings()
        {
            _soundService.Dispose();

            Assert.IsFalse(DependencyContainer.Instance.HasDependency<GlobalAudioSettings>());
            yield return null;
        }
    }
}

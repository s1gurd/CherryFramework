using System;
using System.Collections.Generic;
using CherryFramework.DependencyManager;
using NUnit.Framework;

namespace CherryFramework.Tests
{
    public class DependencyContainerTests
    {
        private class Service
        {
        }

        private class OtherService
        {
        }

        // Every consumer asks for exactly one type, so that a test which
        // binds one service does not make the container complain about the
        // other one.
        private class ServiceConsumer : InjectClass
        {
            [Inject] private Service _service;

            public Service Service => _service;
        }

        private class OtherServiceConsumer : InjectClass
        {
            [Inject] public OtherService Other { get; set; }
        }

        private class DerivedConsumer : ServiceConsumer
        {
            [Inject] private OtherService _inherited;

            public OtherService Inherited => _inherited;
        }

        private readonly List<Type> _bound = new();

        [TearDown]
        public void TearDown()
        {
            // The container logs an error when a type that was never
            // registered is removed, so only the types this test bound go.
            foreach (var type in _bound)
            {
                if (DependencyContainer.Instance.HasDependency(type))
                {
                    DependencyContainer.Instance.RemoveDependency(type);
                }
            }

            _bound.Clear();
        }

        private void Bind<T>(T instance) where T : class
        {
            DependencyContainer.Instance.BindAsSingleton(instance);
            _bound.Add(typeof(T));
        }

        private void Bind<T>() where T : class, new()
        {
            DependencyContainer.Instance.BindAsSingleton<T>();
            _bound.Add(typeof(T));
        }

        [Test]
        public void BindAsSingleton_Instance_IsInjectedIntoTheField()
        {
            var service = new Service();
            Bind(service);

            var consumer = new ServiceConsumer();

            Assert.AreSame(service, consumer.Service);
        }

        [Test]
        public void BindAsSingleton_WithoutAnInstance_ReusesOneInstance()
        {
            Bind<Service>();

            var first = new ServiceConsumer();
            var second = new ServiceConsumer();

            Assert.IsNotNull(first.Service);
            Assert.AreSame(first.Service, second.Service);
        }

        [Test]
        public void Bind_Transient_CreatesANewInstanceForEveryInjection()
        {
            DependencyContainer.Instance.Bind<Service>(BindingType.Transient);
            _bound.Add(typeof(Service));

            var first = new ServiceConsumer();
            var second = new ServiceConsumer();

            Assert.IsNotNull(first.Service);
            Assert.AreNotSame(first.Service, second.Service);
        }

        [Test]
        public void BindAsSingleton_InterfaceBinding_ResolvesByTheServiceType()
        {
            var service = new OtherService();
            DependencyContainer.Instance.BindAsSingleton<OtherService, OtherService>(service);
            _bound.Add(typeof(OtherService));

            var consumer = new OtherServiceConsumer();

            Assert.AreSame(service, consumer.Other);
        }

        [Test]
        public void InjectDependencies_PropertyWithTheAttribute_IsSet()
        {
            var other = new OtherService();
            Bind(other);

            var consumer = new OtherServiceConsumer();

            Assert.AreSame(other, consumer.Other);
        }

        [Test]
        public void InjectDependencies_FieldsOfTheBaseClass_AreInjected()
        {
            var service = new Service();
            var other = new OtherService();
            Bind(service);
            Bind(other);

            var consumer = new DerivedConsumer();

            Assert.AreSame(service, consumer.Service);
            Assert.AreSame(other, consumer.Inherited);
        }

        [Test]
        public void RemoveDependency_ThenHasDependency_IsFalse()
        {
            Bind(new Service());

            DependencyContainer.Instance.RemoveDependency(typeof(Service));

            Assert.IsFalse(DependencyContainer.Instance.HasDependency<Service>());
        }

        [Test]
        public void Dispose_ClearsTheRegistrations()
        {
            Bind(new Service());

            DependencyContainer.Instance.Dispose();

            Assert.IsFalse(DependencyContainer.Instance.HasDependency<Service>());
        }
    }
}

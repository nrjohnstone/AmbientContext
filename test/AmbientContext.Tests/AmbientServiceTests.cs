using System;
using FluentAssertions;
using Xunit;

namespace AmbientContext.Tests
{
    public class AmbientServiceTests : IDisposable
    {
        private interface IFoo
        {
        }

        private class Foo : IFoo
        {
        }

        private class Foo2 : IFoo { }

        private class AmbientServiceNoDefault : AmbientService<IFoo>
        {
            
        }

        private class AmbientServiceWithDefault : AmbientService<IFoo>
        {
            protected override IFoo DefaultCreate()
            {
                return new Foo();
            }
        }

        public void Dispose()
        {
            AmbientServiceNoDefault.Create = () => null;
        }

        [Fact]
        public void Instance_WhenNoDefaultCreateOrCreateSet_ShouldThrow()
        {
            var sut = new AmbientServiceNoDefault();

            Action instance = () =>
            {
                var x = sut.Instance; 
            };

            instance.ShouldThrow<Exception>();
        }

        [Fact]
        public void Instance_WhenCreateDelegateSupplied_ShouldReturnInstance()
        {
            AmbientServiceNoDefault.Create = () => new Foo();

            var sut = new AmbientServiceNoDefault();
            
            var instance = sut.Instance;
            
            instance.Should().BeOfType<Foo>();
        }

        [Fact]
        public void Instance_WhenDefaultDelegateSupplied_ShouldReturnInstance()
        {
            var sut = new AmbientServiceWithDefault();

            var instance = sut.Instance;

            instance.Should().BeOfType<Foo>();
        }

        [Fact]
        public void Instance_WhenDefaultDelegateSuppliedAndCreateSet_ShouldReturnCreateInstance()
        {
            AmbientServiceWithDefault.Create = () => new Foo2();
            var sut = new AmbientServiceWithDefault();

            var instance = sut.Instance;

            instance.Should().BeOfType<Foo2>();
        }

        [Fact]
        public void Instance_WhenInstanceSet_ShouldReturnInstance()
        {
            AmbientServiceWithDefault.Create = () => new Foo2();
            var sut = new AmbientServiceWithDefault();
            sut.Instance = new Foo();

            var instance = sut.Instance;

            instance.Should().BeOfType<Foo>();
        }

        [Fact]
        public void Instance_WhenInstanceSetToNull_ShouldThrow()
        {
            AmbientServiceWithDefault.Create = () => new Foo2();
            var sut = new AmbientServiceWithDefault();

            Action setInstanceToNull = () => sut.Instance = null;

            setInstanceToNull.ShouldThrow<ArgumentNullException>();
        }
    }
}

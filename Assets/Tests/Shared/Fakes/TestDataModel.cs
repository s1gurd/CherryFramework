using System;
using CherryFramework.ModelService;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// A data model in the shape the code generator produces: a getter, a
    /// setter that sends the value downwards, and an accessor.
    /// </summary>
    public class TestDataModel : DataModelBase
    {
        private int _health = 100;
        private string _title = "untitled";

        public TestDataModel()
        {
            Getters.Add(nameof(Health), new Func<int>(() => Health));
            Setters.Add(nameof(Health), new Action<int>(value => Health = value));
            HealthAccessor = new Accessor<int>(this, nameof(Health));

            Getters.Add(nameof(Title), new Func<string>(() => Title));
            Setters.Add(nameof(Title), new Action<string>(value => Title = value));
            TitleAccessor = new Accessor<string>(this, nameof(Title));
        }

        public int Health
        {
            get => _health;
            set
            {
                _health = value;
                Send(nameof(Health), value);
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                Send(nameof(Title), value);
            }
        }

        public Accessor<int> HealthAccessor;
        public Accessor<string> TitleAccessor;
    }
}

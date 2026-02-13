// <copyright file="TestTypes.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using UnityEngine;

    // Abstract component for testing
    public abstract class AbstractTestComponent : MonoBehaviour
    {
        public abstract void DoSomething();
    }

    // Concrete component for testing
    public class ConcreteTestComponent : AbstractTestComponent
    {
        [SerializeField]
        private float testFloat = 1.0f;

        [SerializeField]
        private int testInt = 42;

        [SerializeField]
        private string testString = "test";

        [SerializeField]
        private bool testBool = true;

        public float TestFloat
        {
            get => this.testFloat;
            set => this.testFloat = value;
        }

        public int TestInt
        {
            get => this.testInt;
            set => this.testInt = value;
        }

        public string TestString
        {
            get => this.testString;
            set => this.testString = value;
        }

        public bool TestBool
        {
            get => this.testBool;
            set => this.testBool = value;
        }

        public override void DoSomething()
        {
            // Implementation
        }
    }

    // Abstract ScriptableObject for testing
    public abstract class AbstractTestScriptableObject : ScriptableObject
    {
        public abstract string GetData();
    }

    // Concrete ScriptableObject for testing
    public class ConcreteTestScriptableObject : AbstractTestScriptableObject
    {
        [SerializeField]
        private float health = 100.0f;

        [SerializeField]
        private int damage = 25;

        [SerializeField]
        private string itemName = "Test Item";

        public float Health
        {
            get => this.health;
            set => this.health = value;
        }

        public int Damage
        {
            get => this.damage;
            set => this.damage = value;
        }

        public string ItemName
        {
            get => this.itemName;
            set => this.itemName = value;
        }

        public override string GetData()
        {
            return "test data";
        }
    }
}
using System;

namespace Moneyes.Test
{
    public class TestEntity
    {
        public Guid Id { get; init; }

        public string Name { get; init; }

        public int Age { get; init; }

        public string CarNumberPlate { get; init; }

        public override bool Equals(object obj)
        {
            return obj is TestEntity entity &&
                   Id.Equals(entity.Id) &&
                   Name == entity.Name &&
                   Age == entity.Age &&
                   CarNumberPlate == entity.CarNumberPlate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Age, CarNumberPlate);
        }
    }
}
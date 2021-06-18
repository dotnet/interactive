using System;

namespace RandomNumberExtension
{
    public class RandomNumberGenerator
    {
        private Random _rnd = new Random();

        public int GetRandomNumber(int lowerBound, int upperBound)
        {
            return _rnd.Next(lowerBound, upperBound);
        }
    }
}

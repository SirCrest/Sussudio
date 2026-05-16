using System;

namespace Sussudio.Controllers;

internal sealed class SplashLoadingPhrasePacingPolicy
{
    private SplashLoadingPhrasePaceMode _mode;
    private int _ticksLeft;

    public TimeSpan NextInterval()
        => NextInterval(Random.Shared.NextDouble, Random.Shared.Next);

    internal TimeSpan NextInterval(Func<double> nextDouble, Func<int, int, int> nextInt)
    {
        if (_ticksLeft <= 0)
        {
            var roll = nextDouble();
            if (roll < 0.20)
            {
                _mode = SplashLoadingPhrasePaceMode.Burst;
                _ticksLeft = nextInt(2, 6);
            }
            else if (roll < 0.70)
            {
                _mode = SplashLoadingPhrasePaceMode.Normal;
                _ticksLeft = nextInt(1, 4);
            }
            else if (roll < 0.90)
            {
                _mode = SplashLoadingPhrasePaceMode.Stuck;
                _ticksLeft = 1;
            }
            else
            {
                _mode = SplashLoadingPhrasePaceMode.LongStuck;
                _ticksLeft = 1;
            }
        }

        _ticksLeft--;

        var ms = _mode switch
        {
            SplashLoadingPhrasePaceMode.Burst => nextInt(280, 420),
            SplashLoadingPhrasePaceMode.Normal => nextInt(380, 900),
            SplashLoadingPhrasePaceMode.Stuck => nextInt(900, 1500),
            SplashLoadingPhrasePaceMode.LongStuck => nextInt(1500, 2500),
            _ => 600,
        };
        return TimeSpan.FromMilliseconds(ms);
    }

    public void Reset()
    {
        _mode = default;
        _ticksLeft = 0;
    }
}

internal enum SplashLoadingPhrasePaceMode
{
    Burst,
    Normal,
    Stuck,
    LongStuck,
}

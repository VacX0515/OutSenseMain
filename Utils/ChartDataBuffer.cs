using System;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// Thread-safe circular buffer for chart time-series data.
    /// Default capacity = 3600 points (1 hour at 1-second intervals).
    /// </summary>
    public class ChartDataBuffer
    {
        private readonly object _lock = new object();
        private readonly DateTime[] _timestamps;
        private readonly double[] _values;
        private readonly int _capacity;
        private int _count;
        private int _head;

        public ChartDataBuffer(int capacity = 3600)
        {
            _capacity = capacity;
            _timestamps = new DateTime[capacity];
            _values = new double[capacity];
        }

        public void Add(DateTime timestamp, double value)
        {
            lock (_lock)
            {
                _timestamps[_head] = timestamp;
                _values[_head] = value;
                _head = (_head + 1) % _capacity;
                if (_count < _capacity) _count++;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _count = 0;
                _head = 0;
            }
        }

        public int Count
        {
            get { lock (_lock) { return _count; } }
        }

        /// <summary>
        /// Returns chronologically ordered snapshot as (xs, ys) arrays.
        /// xs contains OADate values for ScottPlot DateTime axis.
        /// </summary>
        public (double[] xs, double[] ys) GetData()
        {
            lock (_lock)
            {
                if (_count == 0)
                    return (Array.Empty<double>(), Array.Empty<double>());

                var xs = new double[_count];
                var ys = new double[_count];
                int start = (_head - _count + _capacity) % _capacity;

                for (int i = 0; i < _count; i++)
                {
                    int idx = (start + i) % _capacity;
                    xs[i] = _timestamps[idx].ToOADate();
                    ys[i] = _values[idx];
                }
                return (xs, ys);
            }
        }
    }
}

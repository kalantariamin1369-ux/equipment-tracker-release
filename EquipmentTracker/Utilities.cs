using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace EquipmentTracker
{
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this DataGridView dgv, bool setting)
        {
            Type dgvType = dgv.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pi != null) pi.SetValue(dgv, setting, null);
        }
    }

    public class SortableBindingList<T> : BindingList<T>
    {
        private bool _isSorted;
        private ListSortDirection _sortDirection;
        private PropertyDescriptor _sortProperty;

        public SortableBindingList(IList<T> list) : base(list) { }

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _isSorted;
        protected override ListSortDirection SortDirectionCore => _sortDirection;
        protected override PropertyDescriptor SortPropertyCore => _sortProperty;

        public void Reset(IList<T> newlist)
        {
            ClearItems();
            foreach (var item in newlist)
            {
                Add(item);
            }
            if (_isSorted)
            {
                ApplySortCore(_sortProperty, _sortDirection);
            }
            else
            {
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
        }

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var items = this.Items as List<T>;
            if (items != null)
            {
                var pc = new PropertyComparer<T>(prop, direction);
                items.Sort(pc);
                _isSorted = true;
                _sortDirection = direction;
                _sortProperty = prop;
            }
            else
            {
                _isSorted = false;
            }
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
        }
    }

    public class PropertyComparer<T> : IComparer<T>
    {
        private readonly IComparer _comparer;
        private readonly ListSortDirection _direction;
        private readonly PropertyDescriptor _prop;
        private readonly bool _isString;

        public PropertyComparer(PropertyDescriptor prop, ListSortDirection direction)
        {
            _prop = prop;
            _direction = direction;
            _comparer = Comparer<object>.Default;
            _isString = prop.PropertyType == typeof(string);
        }

        public int Compare(T x, T y)
        {
            var xValue = _prop.GetValue(x);
            var yValue = _prop.GetValue(y);

            int result;
            if (_isString)
            {
                result = string.Compare(xValue as string, yValue as string, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                result = _comparer.Compare(xValue, yValue);
            }

            return _direction == ListSortDirection.Ascending ? result : -result;
        }
    }

    public static class Logger
    {
        private static readonly string _logDirectory;
        private static readonly object _lock = new object();

        static Logger()
        {
            _logDirectory = Path.Combine(AppSettings.GetAppDataPath(), "logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public static void Log(string message, string level = "INFO")
        {
            string logFilePath = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.ToUpper()}] {message}";

            lock (_lock)
            {
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
        }
    }
}


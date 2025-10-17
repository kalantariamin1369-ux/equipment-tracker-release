using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EquipmentTracker
{
    public class Equipment : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private int _quantity;
        private DateTime _lastUpdated;
        private string _category;
        private int _minStockLevel;

        public string Id { get => _id; set { _id = value; OnPropertyChanged(nameof(Id)); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(nameof(Quantity)); } }
        public string Category { get => _category; set { _category = value; OnPropertyChanged(nameof(Category)); } }
        public int MinStockLevel { get => _minStockLevel; set { _minStockLevel = value; OnPropertyChanged(nameof(MinStockLevel)); } }
        public DateTime LastUpdated { get => _lastUpdated; set { _lastUpdated = value; OnPropertyChanged(nameof(LastUpdated)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Transaction
    {
        public int Id { get; set; }
        public string EquipmentId { get; set; }
        public string EquipmentName { get; set; } // For display purposes
        public DateTime Timestamp { get; set; }
        public string ChangeType { get; set; }
        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string Notes { get; set; }
    }
}


# Equipment Tracker

A robust desktop application for managing equipment inventory, built with C# and Windows Forms.

## Features

- **Comprehensive Equipment Management**: Add, update, delete, and track equipment items.
- **Quantity Tracking**: Easily add or remove quantities of items with transaction logging.
- **Categorization**: Organize equipment by categories for better management.
- **Low Stock Alerts**: Define minimum stock levels and receive visual alerts for items running low.
- **Search & Filter**: Quickly find equipment by name or category.
- **Sortable Grid**: Sort equipment list by any column for easy data analysis.
- **In-place Editing**: Directly edit equipment details (name, category, min stock) within the data grid.
- **Transaction History**: View a detailed log of all quantity changes, including who made the change (if authenticated) and notes.
- **SQLite Database**: Persistent data storage using a lightweight, embedded SQLite database.
- **CSV Import/Export**: Import new equipment from CSV files and export current inventory or transaction history to CSV.
- **Theming**: Switch between Light and Dark modes for a personalized user experience.
- **Configurable Settings**: Customize application behavior, including startup tab and theme preferences.
- **Robust Error Handling & Logging**: Comprehensive logging of application events and errors for debugging and auditing.
- **Backup Functionality**: Easily create backups of your database.

## Installation

1.  **Download the latest release**: Go to the [Releases](https://github.com/kalantariamin1369-ux/equipment-tracker-release/releases) page and download the `EquipmentTracker-Windows-x64-v1.0.0.zip` (or the latest version).
2.  **Extract the contents**: Unzip the downloaded file to a folder of your choice (e.g., `C:\Program Files\EquipmentTracker`).
3.  **Run the application**: Double-click `EquipmentTracker.exe` to start the application.

## Usage

### Equipment Tab

- **Add New Item**: Enter equipment name, initial quantity, and category, then click "Add New Item".
- **Update Quantity**: Select an item, enter the quantity to add/remove, and click "Add" or "Remove".
- **Edit Details**: Double-click a cell in the grid to edit the Name, Category, or Min Stock Level. Press Enter to save changes.
- **Delete Item**: Select an item and click "Delete Selected".
- **Search**: Type in the search box to filter equipment by name or category.
- **Sort**: Click on column headers to sort the grid.

### Transaction History Tab

- **View History**: Shows all changes to equipment quantities.
- **Filter History**: Use the search box and date pickers to filter transactions.

### File Menu

- **Import from CSV**: Import new equipment items from a CSV file. The CSV should have columns: `Name,Quantity,Category,MinStockLevel`.
- **Export to CSV**: Export the current view of equipment or transaction history to a CSV file.
- **Backup Database**: Create a backup of your `equipment.db` file.

### Tools Menu

- **Settings**: Configure application theme (Light/Dark) and startup tab.
- **View Logs**: Open the folder containing application logs.

## Development

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8 Development Tools
- NuGet Package Manager

### Building from Source

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/kalantariamin1369-ux/equipment-tracker-release.git
    cd equipment-tracker-release
    ```
2.  **Open in Visual Studio**: Open `EquipmentTracker.sln`.
3.  **Restore NuGet Packages**: Build the project, and Visual Studio should automatically restore packages. If not, right-click the solution in Solution Explorer and select "Restore NuGet Packages".
4.  **Build**: Set the solution configuration to `Release` and build the solution.
5.  **Run**: Press F5 or click "Start" to run the application.

## Contributing

Contributions are welcome! Please feel free to fork the repository, make your changes, and submit a pull request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a history of changes.

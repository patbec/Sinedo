using System;

namespace Sinedo.Flags
{
	/// <summary>
	/// Auflistung von erlaubten Zuständen einer Gruppe.
	/// </summary>
	public enum DownloadState : ushort
	{
		/// <summary>
		/// Der Vorgang wurde hinzugefügt aber nicht gestartet.
		/// </summary>
		Idle = 1,
		/// <summary>
		/// Der Vorgang befindet sich in der Warteschlange.
		/// </summary>
		Queued = 2,
		/// <summary>
		/// Der Vorgang wurde durch den Benutzer abgebrochen.
		/// </summary>
		Canceled = 3,
		/// <summary>
		/// Der Vorgang wurde durch einen Fehler abgebrochen.
		/// </summary>
		Failed = 4,
		/// <summary>
		/// Der Vorgang wird vom Aufgabenplaner ausgeführt.
		/// </summary>
		Running = 5,
		/// <summary>
		/// Der Vorgang wurde erfolgreich abgeschlossen.
		/// </summary>
		Completed = 6,
		/// <summary>
        /// Der Vorgang wird entfernt.
        /// </summary>
		Deleting = 8,
		/// <summary>
		/// Der Vorgang wird angehalten.
		/// </summary>
		Stopping = 9,
		/// <summary>
		/// Die Eingabedatei wird nicht unterstützt.
		/// </summary>
		Unsupported = 10,
	}
}

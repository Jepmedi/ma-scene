using System;

namespace EscapeRoom.Core
{
    /// <summary>
    /// Gemeinsame Schnittstelle für alle Rätsel. Ermöglicht eine einheitliche
    /// Behandlung (Aktivieren, Status, Belohnungs-Ziffer) durch GameManager/CodeManager.
    /// </summary>
    public interface IPuzzle
    {
        int PuzzleId { get; }

        bool IsSolved { get; }

        /// <summary>Wird beim Lösen gefeuert: (puzzleId, digit).</summary>
        event Action<int, int> Solved;

        /// <summary>Schaltet das Rätsel spielbereit (z. B. nach Vorbedingung).</summary>
        void Activate();
    }
}

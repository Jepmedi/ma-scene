using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EscapeRoom.Core
{
    /// <summary>
    /// Zentrales Code-System. Sammelt die Ziffern der einzelnen Rätsel und baut daraus
    /// den Gesamtcode (Standard: 5792). Idempotent pro Rätsel.
    /// </summary>
    public class CodeManager : MonoBehaviour
    {
        public static CodeManager Instance { get; private set; }

        [Tooltip("Reihenfolge der Rätsel-IDs, wie sie den Türcode bilden. Default {2,1,4,3} => 5-7-9-2.")]
        [SerializeField] private int[] codeOrder = { 2, 1, 4, 3 };

        [Tooltip("Anzahl benötigter Rätsel, bis der Code vollständig ist.")]
        [SerializeField] private int requiredPuzzleCount = 4;

        private readonly Dictionary<int, int> _digits = new();

        /// <summary>(puzzleId, digit)</summary>
        public event Action<int, int> DigitSubmitted;

        /// <summary>Vollständiger Code als String (z. B. "5792").</summary>
        public event Action<string> CodeComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SubmitDigit(int puzzleId, int digit)
        {
            if (_digits.ContainsKey(puzzleId))
                return; // bereits gewertet -> idempotent

            _digits[puzzleId] = digit;
            Debug.Log($"[CodeManager] Rätsel {puzzleId} gelöst -> Ziffer {digit}. " +
                      $"Gesammelt: {_digits.Count}/{requiredPuzzleCount}");
            DigitSubmitted?.Invoke(puzzleId, digit);

            if (_digits.Count >= requiredPuzzleCount)
            {
                string code = GetCode();
                Debug.Log($"[CodeManager] Code vollständig: {code}");
                CodeComplete?.Invoke(code);
            }
        }

        /// <summary>Baut den Code in der konfigurierten Reihenfolge. Fehlende Ziffern = '_'.</summary>
        public string GetCode()
        {
            var sb = new StringBuilder();
            foreach (int id in codeOrder)
                sb.Append(_digits.TryGetValue(id, out int d) ? d.ToString() : "_");
            return sb.ToString();
        }

        public bool TryGetDigit(int puzzleId, out int digit) => _digits.TryGetValue(puzzleId, out digit);
    }
}

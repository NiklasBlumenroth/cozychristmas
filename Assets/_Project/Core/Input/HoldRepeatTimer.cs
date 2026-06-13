namespace CozySanta.Core.Input
{
    /// <summary>
    /// Reine Eingabe-Mathematik für „Taste gedrückt halten = Aktion wiederholen". Aus dem
    /// gedrückt/los-Zustand einer Taste und <c>deltaTime</c> entscheidet <see cref="Tick"/>, ob in
    /// diesem Frame ausgelöst werden soll: einmal sofort beim Drücken, danach – nach einer
    /// Anfangsverzögerung – im festen Wiederhol-Intervall, solange gehalten wird. Ohne
    /// UnityEngine-Abhängigkeit; die Runtime füttert den realen Tastenzustand und die Frame-Zeit.
    /// </summary>
    public struct HoldRepeatTimer
    {
        private bool _wasPressed;
        private bool _repeating;
        private float _timer;

        /// <summary>
        /// Decide: <c>true</c> genau in den Frames, in denen ausgelöst werden soll. Der erste
        /// Tastendruck löst sofort aus. Bleibt die Taste gedrückt, beginnt nach
        /// <paramref name="initialDelay"/> die Wiederholung im Abstand <paramref name="repeatInterval"/>.
        /// Loslassen setzt zurück. Nicht-positive Schwellen schalten die Auto-Wiederholung aus.
        /// </summary>
        public bool Tick(bool pressed, float deltaTime, float initialDelay, float repeatInterval)
        {
            if (!pressed)
            {
                _wasPressed = false;
                _repeating = false;
                _timer = 0f;
                return false;
            }

            if (!_wasPressed)
            {
                _wasPressed = true;
                _repeating = false;
                _timer = 0f;
                return true;
            }

            var threshold = _repeating ? repeatInterval : initialDelay;
            if (threshold <= 0f)
            {
                return false;
            }

            _timer += deltaTime;
            if (_timer < threshold)
            {
                return false;
            }

            _timer -= threshold;
            _repeating = true;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Thin platform layer so store builds (Android / iOS) and web portals (itch.io, CrazyGames, Poki) can share one
    /// codebase. Game code only reports "gameplay running / paused" and loading progress; a portal SDK bridge
    /// subscribes to these events later (Poki gameplayStart/Stop, CrazyGames gameplayStart/Stop, loadingStop...).
    /// </summary>
    public static class Platform
    {
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;
        public static bool IsMobile => Application.isMobilePlatform;

        /// <summary>Raised when play resumes (no popup / ad on screen).</summary>
        public static event Action GameplayStarted;
        /// <summary>Raised when a popup, ad or menu interrupts play.</summary>
        public static event Action GameplayStopped;
        public static event Action LoadingFinished;

        static readonly HashSet<string> Pauses = new HashSet<string>();
        static bool _started;
        static bool _loaded;

        public static bool GameplayRunning => _started && Pauses.Count == 0;

        /// <summary>Call once the first frame of gameplay is visible.</summary>
        public static void NotifyLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            LoadingFinished?.Invoke();
        }

        /// <summary>The player has started interacting (first input).</summary>
        public static void NotifyFirstInput()
        {
            if (_started) return;
            _started = true;
            if (Pauses.Count == 0) GameplayStarted?.Invoke();
        }

        /// <summary>Something interrupts play (reason is a unique key such as "offer_popup").</summary>
        public static void Pause(string reason)
        {
            bool wasRunning = GameplayRunning;
            Pauses.Add(reason);
            if (wasRunning) GameplayStopped?.Invoke();
        }

        public static void Resume(string reason)
        {
            if (!Pauses.Remove(reason)) return;
            if (GameplayRunning) GameplayStarted?.Invoke();
        }
    }
}

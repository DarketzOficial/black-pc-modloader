using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlackLauncher
{
    public enum KeySlotSource { None, Keyboard, Mouse, Wheel }

    public struct CapturedKey
    {
        public ushort Vk;
        public KeySlotSource Source;
        public CapturedKey(ushort vk, KeySlotSource src) { Vk = vk; Source = src; }
        public static readonly CapturedKey None = new CapturedKey(0, KeySlotSource.None);
    }

    /// <summary>
    /// Keyboard/mouse keys bound to the game's actions, mirroring the port's input/bindings.cpp:
    /// black.ini holds "key_&lt;action&gt; = Key[,Key]" with the port's own spelling (letters, the named
    /// keys, "F1".."F24", "Num0".."Num9", or "0xNN"); "none" or an empty slot means unbound.
    /// </summary>
    public static class KeyBindings
    {
        public const ushort WheelUp = 0x100;
        public const ushort WheelDown = 0x101;

        public sealed class Action
        {
            public string Ini;
            public string NamePt;
            public string NameEn;
            public ushort[] Defaults = new ushort[2];
            public bool Fixed; // Esc is always the first pause key (bindings.cpp fixed())
        }

        // Same order and defaults as bindings.cpp kActions.
        public static readonly Action[] Actions =
        {
            new Action { Ini = "run_forward",   NameEn = "Run forward",           NamePt = "Correr para frente", Defaults = new ushort[] { (ushort)'W', 0 } },
            new Action { Ini = "run_back",      NameEn = "Run back",              NamePt = "Correr para trás",   Defaults = new ushort[] { (ushort)'S', 0 } },
            new Action { Ini = "step_left",     NameEn = "Sidestep left",         NamePt = "Passo à esquerda",   Defaults = new ushort[] { (ushort)'A', 0 } },
            new Action { Ini = "step_right",    NameEn = "Sidestep right",        NamePt = "Passo à direita",    Defaults = new ushort[] { (ushort)'D', 0 } },
            new Action { Ini = "fire",          NameEn = "Fire",                  NamePt = "Atirar",             Defaults = new ushort[] { 0x01, 0 } },
            new Action { Ini = "aim",           NameEn = "Aim",                   NamePt = "Mirar",              Defaults = new ushort[] { 0x02, 0 } },
            new Action { Ini = "reload",        NameEn = "Reload",                NamePt = "Recarregar",         Defaults = new ushort[] { (ushort)'R', 0 } },
            new Action { Ini = "use",           NameEn = "Action (use, pick up)", NamePt = "Ação (usar, pegar)", Defaults = new ushort[] { (ushort)'E', 0 } },
            new Action { Ini = "crouch",        NameEn = "Crouch",                NamePt = "Agachar",            Defaults = new ushort[] { (ushort)'C', 0 } },
            new Action { Ini = "grenade",       NameEn = "Grenade",               NamePt = "Granada",            Defaults = new ushort[] { (ushort)'G', 0 } },
            new Action { Ini = "melee",         NameEn = "Melee",                 NamePt = "Corpo a corpo",      Defaults = new ushort[] { (ushort)'F', 0 } },
            new Action { Ini = "next_weapon",   NameEn = "Next weapon",           NamePt = "Próxima arma",       Defaults = new ushort[] { WheelDown, 0 } },
            new Action { Ini = "prev_weapon",   NameEn = "Previous weapon",       NamePt = "Arma anterior",      Defaults = new ushort[] { WheelUp, 0 } },
            new Action { Ini = "firing_mode",   NameEn = "Change firing mode",    NamePt = "Modo de tiro",       Defaults = new ushort[] { (ushort)'Z', 0 } },
            new Action { Ini = "suppressor",    NameEn = "Toggle suppressor",     NamePt = "Supressor",          Defaults = new ushort[] { (ushort)'Q', 0 } },
            new Action { Ini = "health_pack",   NameEn = "Health pack",           NamePt = "Kit de vida",        Defaults = new ushort[] { (ushort)'X', 0 } },
            new Action { Ini = "music",         NameEn = "Next music track",      NamePt = "Próxima música",     Defaults = new ushort[] { 0, 0 } },
            new Action { Ini = "turn_left",     NameEn = "Turn left",             NamePt = "Girar à esquerda",   Defaults = new ushort[] { 0, 0 } },
            new Action { Ini = "turn_right",    NameEn = "Turn right",            NamePt = "Girar à direita",    Defaults = new ushort[] { 0, 0 } },
            new Action { Ini = "look_up",       NameEn = "Look up",               NamePt = "Olhar para cima",    Defaults = new ushort[] { 0, 0 } },
            new Action { Ini = "look_down",     NameEn = "Look down",             NamePt = "Olhar para baixo",   Defaults = new ushort[] { 0, 0 } },
            new Action { Ini = "pause",         NameEn = "Pause",                 NamePt = "Pausar",             Defaults = new ushort[] { 0x1B, 0 }, Fixed = true },
            new Action { Ini = "objectives",    NameEn = "Show objectives",       NamePt = "Ver objetivos",      Defaults = new ushort[] { 0x20, 0 } },
            new Action { Ini = "walk",          NameEn = "Walking",               NamePt = "Andar devagar",      Defaults = new ushort[] { 0xA0, 0 } },
        };

        private struct Named { public ushort Vk; public string Name; }
        private static readonly Named[] NamedKeys =
        {
            new Named { Vk = 0x01, Name = "Mouse1" }, new Named { Vk = 0x02, Name = "Mouse2" }, new Named { Vk = 0x04, Name = "Mouse3" },
            new Named { Vk = 0x05, Name = "Mouse4" }, new Named { Vk = 0x06, Name = "Mouse5" },
            new Named { Vk = WheelUp, Name = "WheelUp" }, new Named { Vk = WheelDown, Name = "WheelDown" },
            new Named { Vk = 0x20, Name = "Space" }, new Named { Vk = 0x0D, Name = "Enter" }, new Named { Vk = 0x1B, Name = "Esc" },
            new Named { Vk = 0x09, Name = "Tab" }, new Named { Vk = 0x08, Name = "Backspace" },
            new Named { Vk = 0x10, Name = "Shift" }, new Named { Vk = 0x11, Name = "Ctrl" }, new Named { Vk = 0x12, Name = "Alt" },
            new Named { Vk = 0xA0, Name = "LShift" }, new Named { Vk = 0xA1, Name = "RShift" },
            new Named { Vk = 0xA2, Name = "LCtrl" }, new Named { Vk = 0xA3, Name = "RCtrl" },
            new Named { Vk = 0xA4, Name = "LAlt" }, new Named { Vk = 0xA5, Name = "RAlt" },
            new Named { Vk = 0x14, Name = "CapsLock" },
            new Named { Vk = 0x26, Name = "Up" }, new Named { Vk = 0x28, Name = "Down" }, new Named { Vk = 0x25, Name = "Left" }, new Named { Vk = 0x27, Name = "Right" },
            new Named { Vk = 0x2D, Name = "Insert" }, new Named { Vk = 0x2E, Name = "Delete" }, new Named { Vk = 0x24, Name = "Home" },
            new Named { Vk = 0x23, Name = "End" }, new Named { Vk = 0x21, Name = "PageUp" }, new Named { Vk = 0x22, Name = "PageDown" },
            new Named { Vk = 0x6A, Name = "Num*" }, new Named { Vk = 0x6B, Name = "Num+" }, new Named { Vk = 0x6D, Name = "Num-" },
            new Named { Vk = 0x6E, Name = "Num." }, new Named { Vk = 0x6F, Name = "Num/" },
            new Named { Vk = 0x13, Name = "Pause" }, new Named { Vk = 0x91, Name = "ScrollLock" }, new Named { Vk = 0x90, Name = "NumLock" },
            new Named { Vk = 0x5D, Name = "Menu" },
        };

        /// <summary>black.ini spelling of a key (bindings.cpp ini_name).</summary>
        public static string IniName(ushort k)
        {
            if (k == 0) return "none";
            foreach (var n in NamedKeys) if (n.Vk == k) return n.Name;
            if ((k >= 'A' && k <= 'Z') || (k >= '0' && k <= '9')) return ((char)k).ToString();
            if (k >= 0x70 && k <= 0x87) return "F" + (k - 0x70 + 1);
            if (k >= 0x60 && k <= 0x69) return "Num" + (k - 0x60);
            return "0x" + k.ToString("X2", CultureInfo.InvariantCulture);
        }

        /// <summary>Parse one black.ini key spelling (bindings.cpp parse_key). 0 = none.</summary>
        public static ushort ParseKey(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            s = s.Trim();
            if (s.Length == 0 || s.Equals("none", StringComparison.OrdinalIgnoreCase)) return 0;
            foreach (var n in NamedKeys) if (string.Equals(n.Name, s, StringComparison.OrdinalIgnoreCase)) return n.Vk;
            if (s.Length == 1 && char.IsLetterOrDigit(s[0])) return (ushort)char.ToUpperInvariant(s[0]);
            if ((s[0] == 'F' || s[0] == 'f') && s.Length <= 3 && s.Length > 1 && char.IsDigit(s[1]))
            {
                int v; if (int.TryParse(s.Substring(1), out v) && v >= 1 && v <= 24) return (ushort)(0x70 + v - 1);
            }
            if (s.Length == 4 && s.Substring(0, 3).Equals("Num", StringComparison.OrdinalIgnoreCase) && char.IsDigit(s[3]))
                return (ushort)(0x60 + (s[3] - '0'));
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                ushort v; if (ushort.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return v;
            }
            return 0;
        }

        /// <summary>Human name shown to the player (bindings.cpp key_name), in the launcher's language.</summary>
        public static string DisplayName(ushort k, bool pt)
        {
            if (k == 0) return pt ? "—" : "—";
            switch (k)
            {
                case 0x01: return pt ? "Botão esquerdo" : "Left mouse button";
                case 0x02: return pt ? "Botão direito" : "Right mouse button";
                case 0x04: return pt ? "Botão do meio" : "Middle mouse button";
                case 0x05: return pt ? "Botão 4" : "Mouse button 4";
                case 0x06: return pt ? "Botão 5" : "Mouse button 5";
                case WheelUp: return pt ? "Roda para cima" : "Mouse wheel up";
                case WheelDown: return pt ? "Roda para baixo" : "Mouse wheel down";
                case 0xA0: return pt ? "Shift esquerdo" : "Left Shift";
                case 0xA1: return pt ? "Shift direito" : "Right Shift";
                case 0xA2: return pt ? "Ctrl esquerdo" : "Left Ctrl";
                case 0xA3: return pt ? "Ctrl direito" : "Right Ctrl";
                case 0xA4: return pt ? "Alt esquerdo" : "Left Alt";
                case 0xA5: return pt ? "Alt direito" : "Right Alt";
            }
            return IniName(k);
        }

        /// <summary>The two slots for an action as stored in black.ini (key_&lt;ini&gt; = a,b).</summary>
        public static ushort[] ReadSlots(IniFile ini, Action a, Func<string, string> get)
        {
            ushort[] slots = { a.Defaults[0], a.Defaults[1] };
            string raw = get("key_" + a.Ini);
            if (raw == null) return slots;
            string[] parts = raw.Split(',');
            for (int i = 0; i < 2; i++)
                slots[i] = (ushort)(i < parts.Length ? ParseKey(parts[i]) : 0);
            if (a.Fixed) slots[0] = 0x1B;
            return slots;
        }

        public static string WriteSlots(ushort[] slots)
        {
            string v = IniName(slots[0]);
            if (slots.Length > 1 && slots[1] != 0) v += "," + IniName(slots[1]);
            return v;
        }
    }
}

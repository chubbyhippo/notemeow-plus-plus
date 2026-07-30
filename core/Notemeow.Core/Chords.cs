// Copyright (C) 2026 Chubby Hippo
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
// more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <https://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Notemeow.Core
{
    public static class Chords
    {
        public static Rc.Binding BindingFor(Chord chord)
        {
            if (chord == null) return null;
            return Rc.ChordBindings().TryGetValue(chord, out Rc.Binding binding) ? binding : null;
        }

        public static bool Claims(MeowMode mode, Chord chord)
        {
            if (!mode.TakesChords()) return false;
            return BindingFor(chord) != null;
        }

        public static bool Dispatch(Ctx ctx, Chord chord)
        {
            if (!Claims(ctx.St.Mode, chord)) return false;
            Engine.RunBinding(ctx, BindingFor(chord));
            return true;
        }
    }
}

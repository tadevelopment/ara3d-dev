// MIT License 
// Copyright (C) 2018 Ara 3D. Inc
// Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace Ara3D
{
    public static class Hash
    {
        // Discussion: if we want to go deeper into the subject, check out
        // https://en.wikipedia.org/wiki/List_of_hash_functions
        // https://stackoverflow.com/questions/5889238/why-is-xor-the-default-way-to-combine-hashes
        // https://en.wikipedia.org/wiki/Jenkins_hash_function#cite_note-11

        public static int Combine(int h1, int h2)
        {
            unchecked
            {
                // RyuJIT optimizes this to use the ROL instruction
                // Related GitHub pull request: dotnet/coreclr#1830
                uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                return ((int)rol5 + h1) ^ h2;
            }
        }

        public static int Combine(params int[] xs)
        {
            if (xs.Length == 0) return 0;
            var r = xs[0];
            for (var i = 1; i < xs.Length; ++i)
                r = Combine(r, i);
            return r;
        }

    }
}

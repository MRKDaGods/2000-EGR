using System;
using System.Collections.Generic;

namespace MRK {
    public class MRKProfile {
        class Profile {
            public long Start;
            public string Tag;
        }

        readonly static Stack<Profile> ms_ProfileStack;

        static MRKProfile() {
            ms_ProfileStack = new Stack<Profile>();
        }

        public static void Push(string tag) {
            Profile profile = ObjectPool<Profile>.Default.Rent();
            profile.Tag = tag;
            profile.Start = DateTime.Now.Ticks;
            ms_ProfileStack.Push(profile);
        }

        public static void Pop() {
            long now = DateTime.Now.Ticks;
            Profile profile = ms_ProfileStack.Pop();
            MRKLogger.Log($"PROFILE: {profile.Tag} - {new TimeSpan(now - profile.Start).TotalMilliseconds}ms");
            ObjectPool<Profile>.Default.Free(profile);
        }
    }
}

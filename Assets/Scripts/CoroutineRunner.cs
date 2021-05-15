using System.Collections;
using UnityEngine;

namespace MRK {
    public class CoroutineRunner : MonoBehaviour {
        public void Run(IEnumerator coroutine) {
            StartCoroutine(coroutine);
        }
    }
}

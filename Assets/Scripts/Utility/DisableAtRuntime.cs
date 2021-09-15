namespace MRK {
    public class DisableAtRuntime : MRKBehaviour {
        void Awake() {
            gameObject.SetActive(false);
        }
    }
}

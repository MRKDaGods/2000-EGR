namespace MRK.UI.Usables
{
    public class Usable : BaseBehaviour
    {
        public Usable Get()
        {
            return Instantiate(this);
        }
    }
}

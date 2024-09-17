using System;
using UnityEngine;

namespace MRK
{
    public enum PlanetType
    {
        None,
        Sun,
        Mercury,
        Venus,
        Earth,
        Mars,
        Jupiter,
        Saturn,
        Uranus,
        Neptune
    }

    public class Planet : BaseBehaviour
    {
        [SerializeField]
        private PlanetType _planetType;
        [SerializeField]
        private float _rotationSpeed;
        private GameObject _halo;

        public static Planet Sun
        {
            get; private set;
        }

        public PlanetType PlanetType
        {
            get
            {
                return _planetType;
            }
        }

        private void Awake()
        {
            if (_planetType == PlanetType.Sun)
            {
                Sun = this;
            }

            _halo = transform.Find("Halo").gameObject;
        }

        private void Start()
        {
            //random move
            if (_planetType == PlanetType.Sun || _planetType == PlanetType.Earth)
            {
                return;
            }

            //rotate a random value around sun INITIALLY
            transform.RotateAround(Sun.transform.position, Vector3.up, UnityEngine.Random.value * 360f);
        }

        private void Update()
        {
            if (_planetType == PlanetType.Sun)
            {
                return;
            }

            transform.RotateAround(Sun.transform.position, Vector3.up, _rotationSpeed * Time.deltaTime);
        }

        private void OnValidate()
        {
            PlanetType pt;
            if (Enum.TryParse(name, out pt))
            {
                _planetType = pt;
            }
        }

        public void SetHaloActiveState(bool active)
        {
            _halo.SetActive(active);
        }
    }
}

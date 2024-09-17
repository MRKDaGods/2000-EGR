using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MRK.Cryptography
{
    public class CryptoPlayerPrefs
    {
        private enum StackValueType
        {
            Byte,
            Bool,
            Int16,
            Int32,
            Int64,
            UInt16,
            UInt32,
            UInt64,
            String,
            Float32, //single
            Float64 //double
        }

        private struct StackValue
        {
            public object RawValue;
            public StackValueType ValueType;

            public T GetValue<T>()
            {
                return (T)RawValue;
            }
        }

        private static byte[] _localEnergy;
        private static readonly Dictionary<string, StackValue> _stack;
        private static readonly Dictionary<Type, Tuple<Func<byte[], int, object>, Func<object, byte[]>>> _typeConverters;
        private static readonly Dictionary<Type, StackValueType> _stackValueTypeMapping;

        private static Tuple<Func<byte[], int, object>, Func<object, byte[]>> ConstructConverters(Func<byte[], int, object> from, Func<object, byte[]> to)
        {
            return new Tuple<Func<byte[], int, object>, Func<object, byte[]>>(from, to);
        }

        static CryptoPlayerPrefs()
        {
            _stack = new Dictionary<string, StackValue>();

            _typeConverters = new Dictionary<Type, Tuple<Func<byte[], int, object>, Func<object, byte[]>>>() {
                {
                    typeof(byte), ConstructConverters((arr, idx) => {
                        return arr[0];
                    }, (obj) => {
                        return new byte[1] { (byte)obj };
                    })
                },
                {
                    typeof(bool), ConstructConverters((arr, idx) => {
                        return BitConverter.ToBoolean(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((bool)obj);
                    })
                },
                {
                    typeof(short), ConstructConverters((arr, idx) => {
                        return BitConverter.ToInt16(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((short)obj);
                    })
                },
                {
                    typeof(int), ConstructConverters((arr, idx) => {
                        return BitConverter.ToInt32(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((int)obj);
                    })
                },
                {
                    typeof(long), ConstructConverters((arr, idx) => {
                        return BitConverter.ToInt64(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((long)obj);
                    })
                },
                {
                    typeof(ushort), ConstructConverters((arr, idx) => {
                        return BitConverter.ToUInt16(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((ushort)obj);
                    })
                },
                {
                    typeof(uint), ConstructConverters((arr, idx) => {
                        return BitConverter.ToUInt32(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((uint)obj);
                    })
                },
                {
                    typeof(ulong), ConstructConverters((arr, idx) => {
                        return BitConverter.ToUInt64(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((ulong)obj);
                    })
                },
                {
                    typeof(string), ConstructConverters((arr, idx) => {
                        return Encoding.UTF8.GetString(arr);
                    }, (obj) => {
                        return Encoding.UTF8.GetBytes((string)obj);
                    })
                },
                {
                    typeof(float), ConstructConverters((arr, idx) => {
                        return BitConverter.ToSingle(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((float)obj);
                    })
                },
                {
                    typeof(double), ConstructConverters((arr, idx) => {
                        return BitConverter.ToDouble(arr, idx);
                    }, (obj) => {
                        return BitConverter.GetBytes((double)obj);
                    })
                }
            };

            _stackValueTypeMapping = new Dictionary<Type, StackValueType> {
                {
                    typeof(byte), StackValueType.Byte
                },
                {
                    typeof(bool), StackValueType.Bool
                },
                {
                    typeof(short), StackValueType.Int16
                },
                {
                    typeof(int), StackValueType.Int32
                },
                {
                    typeof(long), StackValueType.Int64
                },
                {
                    typeof(ushort), StackValueType.UInt16
                },
                {
                    typeof(uint), StackValueType.UInt32
                },
                {
                    typeof(ulong), StackValueType.UInt64
                },
                {
                    typeof(string), StackValueType.String
                },
                {
                    typeof(float), StackValueType.Float32
                },
                {
                    typeof(double), StackValueType.Float64
                }
            };
        }

        public static void Init()
        {
            //hwid+cookedsalt
            _localEnergy = Crypto.GetUTF8Mem(SystemInfo.deviceUniqueIdentifier);
            Crypto.Cook(_localEnergy, Crypto.GetUTF8Mem(Crypto.CookedSalt));
        }

        private static bool InterlockedCheckStackKeyExistance(string key, Reference<StackValue> val)
        {
            Reference<bool> ret = ReferencePool<bool>.Default.Rent();
            InterlockedStackOperation(() =>
            {
                bool contains = false;
                StackValue _val;
                if (_stack.TryGetValue(key, out _val))
                {
                    contains = true;

                    if (val != null)
                    {
                        val.Value = _val;
                    }
                }

                ret.Value = contains;
            });

            bool exists = ret.Value;
            ReferencePool<bool>.Default.Free(ret);
            return exists;
        }

        private static bool GetLocal<T>(string key, Func<byte[], int, object> converter, ref T val)
        {
            string cryptoKey = Crypto.Hash(key);
            string rawData = PlayerPrefs.GetString(cryptoKey, string.Empty);
            if (rawData.Length == 0)
            {
                return false;
            }

            byte[] data = Convert.FromBase64String(rawData); //MRKCryptography.GetUTF8Mem(rawData);
            Crypto.Cook(data, _localEnergy);
            val = (T)converter(data, 0);

            return true;
        }

        public static void Set<T>(string key, T val)
        {
            InterlockedStackOperation(() =>
            {
                _stack[key] = new StackValue
                {
                    ValueType = _stackValueTypeMapping[typeof(T)],
                    RawValue = val
                };
            });
        }

        public static T Get<T>(string key, T defaultValue = default)
        {
            //check if key is in stack
            T ret = defaultValue;

            Reference<StackValue> stackRef = ReferencePool<StackValue>.Default.Rent();
            if (InterlockedCheckStackKeyExistance(key, stackRef))
            {
                ret = stackRef.Value.GetValue<T>();
                goto __exit;
            }

            //fetch locally!!
            GetLocal(key, _typeConverters[typeof(T)].Item1, ref ret);

        __exit:
            ReferencePool<StackValue>.Default.Free(stackRef);
            return ret;
        }

        private static Type GetTypeFromStackValueType(StackValueType type)
        {
            return _stackValueTypeMapping.First(x => x.Value == type).Key;
        }

        public static void Save()
        {
            InterlockedStackOperation(() =>
            {
                foreach (KeyValuePair<string, StackValue> pair in _stack)
                {
                    string cryptoKey = Crypto.Hash(pair.Key);
                    byte[] rawData = _typeConverters[GetTypeFromStackValueType(pair.Value.ValueType)].Item2(pair.Value.RawValue);
                    Crypto.Cook(rawData, _localEnergy);

                    string oString = Convert.ToBase64String(rawData);
                    PlayerPrefs.SetString(cryptoKey, oString);
                }

                _stack.Clear();
                PlayerPrefs.Save();
            });
        }

        private static void InterlockedStackOperation(Action op)
        {
            lock (_stack)
            {
                op();
            }
        }
    }
}

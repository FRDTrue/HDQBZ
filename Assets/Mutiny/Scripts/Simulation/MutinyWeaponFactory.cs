using System;
using UnityEngine;

namespace Mutiny.Simulation
{
    public static class MutinyWeaponFactory
    {
        public static float GetTwangMaxForce(string weaponType)
        {
            if (string.Equals(weaponType, "banana", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponType, "parachuteBomb", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponType, "rumBottle", StringComparison.OrdinalIgnoreCase))
                return 30f;

            return MutinyPhysics.DefaultTwangMaxForce;
        }

        public static float GetPredictionWeight(string weaponType)
        {
            return string.Equals(weaponType, "boulder", StringComparison.OrdinalIgnoreCase)
                ? 1.5f
                : MutinyPhysics.Gravity;
        }

        public static MutinyWeapon SpawnWeapon(string weaponType, MutinyCharacter owner)
        {
            if (string.IsNullOrEmpty(weaponType) || owner == null)
                return null;

            string norm = weaponType.Trim();
            GameObject go = new GameObject($"Weapon_{norm}");
            go.transform.position = owner.transform.position;

            MutinyWeapon weapon = null;

            if (norm.Equals("cherryBomb", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyCherryBomb>();
            else if (norm.Equals("dynamite", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyDynamite>();
            else if (norm.Equals("banana", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyBanana>();
            else if (norm.Equals("cannonball", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyCannonball>();
            else if (norm.Equals("cannon", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyCannon>();
            else if (norm.Equals("boulder", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyBoulder>();
            else if (norm.Equals("gunpowderBarrel", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyGunpowderBarrel>();
            else if (norm.Equals("mine", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyMine>();
            else if (norm.Equals("parachuteBomb", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyParachuteBomb>();
            else if (norm.Equals("piecesOfEight", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyPiecesOfEight>();
            else if (norm.Equals("rumBottle", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyRumBottle>();
            else if (norm.Equals("seagull", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinySeagull>();
            else if (norm.Equals("tidalWave", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyTidalWave>();
            else if (norm.Equals("voodooDoll", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyVoodooDoll>();
            else if (norm.Equals("woodenCrate", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyWoodenCrate>();
            else if (norm.Equals("anchor", StringComparison.OrdinalIgnoreCase))
                weapon = go.AddComponent<MutinyAnchor>();
            else
            {
                Debug.LogError($"[MutinyWeaponFactory] No runtime implementation is registered for '{norm}'.");
                UnityEngine.Object.Destroy(go);
                return null;
            }

            weapon.Initialize(owner);
            weapon.PrepareForEquip();
            return weapon;
        }

        public static MutinyWeapon SpawnAndLaunch(string weaponType, MutinyCharacter owner, Vector2 startPx, Vector2 dragPx)
        {
            var weapon = SpawnWeapon(weaponType, owner);
            if (weapon != null)
            {
                weapon.Twang(startPx, dragPx);
                owner.ConsumeWeapon(weaponType);
                Mutiny.Presentation.MutinyAudioManager.Instance?.PlaySFX("click");
            }
            return weapon;
        }

        public static MutinyWeapon SpawnAndFire(string weaponType, MutinyCharacter owner, Vector2 velocity,
            bool consumeInventory = true)
        {
            var weapon = SpawnWeapon(weaponType, owner);
            if (weapon != null)
            {
                weapon.Fire(velocity);
                if (consumeInventory)
                    owner.ConsumeWeapon(weaponType);
                Mutiny.Presentation.MutinyAudioManager.Instance?.PlaySFX("click");
            }
            return weapon;
        }
    }
}

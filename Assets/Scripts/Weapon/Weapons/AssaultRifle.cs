[System.Serializable]
public class AssaultRifle : Weapon
{
    public AssaultRifle()
    {
        weaponName = "Assault Rifle";
        damage = 30f;
        fireRate = 0.1f;
        magazineSize = 30;
        reloadTime = 2.5f;
        aimSpread = 0.05f;
        hipSpread = 0.4f;
        recoilKickback = 0.15f;
        maxRecoil = 0.8f;
        shootingMode = ShootingMode.Auto;
    }
}
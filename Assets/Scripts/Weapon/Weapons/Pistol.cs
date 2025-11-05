[System.Serializable]
public class Pistol : Weapon
{
    public Pistol()
    {
        weaponName = "Pistol";
        damage = 40f;
        fireRate = 0.3f;
        magazineSize = 12;
        reloadTime = 1.8f;
        aimSpread = 0.02f;
        hipSpread = 0.7f;
        recoilKickback = 0.25f;
        maxRecoil = 1.0f;
        shootingMode = ShootingMode.SemiAuto;
    }
}
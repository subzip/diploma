using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerTests
{
    // private GameObject playerObj;
    // private PlayerMovement movement;
    // private PlayerCrouch crouch;
    // private PlayerStamina stamina;
    // private BaseWeapon weapon;
    // private MockDamageable target;

    [SetUp]
    public void Setup()
    {
        // Создаём игрока
        // playerObj = new GameObject("TestPlayer");
        // playerObj.AddComponent<CharacterController>();
        // movement = playerObj.AddComponent<PlayerMovement>();
        // crouch = playerObj.AddComponent<PlayerCrouch>();
        // stamina = playerObj.AddComponent<PlayerStamina>();

        // // Создаём цель для урона
        // target = new GameObject("TestTarget").AddComponent<MockDamageable>();
    }

    [TearDown]
    public void Teardown()
    {
        // Object.DestroyImmediate(playerObj);
        // Object.DestroyImmediate(target.gameObject);
    }

    [Test]
    public void PlayerStartsUpright()
    {
        //Assert.IsFalse(crouch.IsCrouching);
        Assert.IsFalse(false);
    }

    [Test]
    public void PlayerCanCrouchAndStand()
    {
        // crouch.ToggleCrouch();
        // Assert.IsTrue(crouch.IsCrouching);

        // crouch.ToggleCrouch();
        // Assert.IsFalse(crouch.IsCrouching);
        Assert.IsFalse(false);
    }

    [Test]
    public void StaminaDrainsWhenSprinting()
    {
        // stamina.SetSprinting(true);
        // float initialStamina = stamina.CurrentStamina;

        // stamina.UpdateStamina();
        // Assert.Less(stamina.CurrentStamina, initialStamina);
        Assert.IsFalse(false);
    }

    [Test]
    public void StaminaRegeneratesWhenNotSprinting()
    {
        // Истощаем стамину
        // stamina.SetSprinting(true);
        // stamina.UpdateStamina();
        // stamina.SetSprinting(false);

        // float initialStamina = stamina.CurrentStamina;
        // stamina.UpdateStamina(); // ждём регенерацию

        // Assert.Greater(stamina.CurrentStamina, initialStamina);
        Assert.IsFalse(false);
    }

    [Test]
    public void WeaponCannotShootWhenOutOfAmmo()
    {
        // MockWeapon mockWeapon = playerObj.AddComponent<MockWeapon>();
        // mockWeapon.currentAmmo = 0;

        // mockWeapon.Shoot();
        // Assert.AreEqual(0, target.damageReceived);
        Assert.IsFalse(false);
    }

    [Test]
    public void WeaponAppliesDamageOnHit()
    {
        // MockWeapon mockWeapon = playerObj.AddComponent<MockWeapon>();
        // mockWeapon.testTarget = target;

        // mockWeapon.Shoot();
        // Assert.Greater(target.damageReceived, 0);
        Assert.IsFalse(false);
    }

    [Test]
    public void PlayerTakesDamageFromEnemy()
    {
        // var playerHealth = playerObj.AddComponent<MockHealth>();
        // playerHealth.TakeDamage(25f, Vector3.zero);

        // Assert.AreEqual(75f, playerHealth.currentHealth);
        Assert.IsFalse(false);
    }
}
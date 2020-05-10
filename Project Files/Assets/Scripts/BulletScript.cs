using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script for controlling a bullet.
/// </summary>
public class BulletScript : MonoBehaviour
{
    // ============================================= Variables
    public float damage = 0.0f;
    public string shooter = "";
    public AudioClip hit;

    // ============================================= Methods
    /// <summary>
    /// On frame update, move bullet.
    /// </summary>
    void Update()
    {
        // Move the bullet forward
        transform.position += transform.forward * 3.0f;
    }

    /// <summary>
    /// On trigger enter, check if an enemy.
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        // Print the other tag
        Debug.Log(other.gameObject.tag);

        // If the other is an enemy, damage it
        if (other.gameObject.CompareTag("Enemy") && shooter != "Enemy")
        {
            other.gameObject.GetComponent<EnemyAI>().Damage(damage);
            AudioSource.PlayClipAtPoint(hit, transform.position);
        }
        // If the other is a player, damage it
        else if (other.gameObject.CompareTag("Player") && shooter != "Player")
        {
            // If we have a player controller, damage that
            if (other.gameObject.TryGetComponent<PlayerController>(out PlayerController controller))
                controller.Damage(damage);
            else
                other.gameObject.GetComponent<PlayerAI>().Damage(damage);
            AudioSource.PlayClipAtPoint(hit, transform.position);
        }

        // Destroy this object, if not another bullet or a goal
        if (!other.gameObject.CompareTag("Bullet") && !other.gameObject.CompareTag("Goal"))
            Destroy(gameObject);
    }
}

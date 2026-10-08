using UnityEngine;

/// <summary>
/// Ancre de placement : un GameObject vide dont la pose est enregistree par le
/// systeme de presets.
///
/// Le probleme qu'elle resout : Unity restaure l'etat serialise des objets de
/// scene en sortie de Play, donc une ancre deplacee pendant un show est perdue.
/// Les presets, eux, ecrivent dans ProjectSettings/PresetsBank.json, sur disque.
/// En capturant 'position' et 'euler' plutot que le Transform, la pose survit.
///
/// La synchronisation va dans les deux sens :
///   • tu deplaces l'objet a la main  -> les champs suivent (tu places a vue) ;
///   • un preset ecrit les champs     -> l'objet suit (rappel).
/// Le Transform est prioritaire : tant que tu le manipules, il fait foi.
///
/// Capture (onglet Capture du Preset Bank) : ce composant, mode Properties,
/// cocher Position et Euler. Surtout PAS le Transform en mode Whole, qui
/// embarque m_Father / m_Children par instanceID et ne survit pas a un
/// rechargement de scene.
/// </summary>
[ExecuteAlways]
public class PresetAnchor : MonoBehaviour
{
    [Tooltip("Position locale capturee par le preset.")]
    public Vector3 position;

    [Tooltip("Rotation locale (angles d'Euler) capturee par le preset.")]
    public Vector3 euler;

    // Dernieres valeurs vues de chaque cote, pour savoir qui a bouge.
    private Vector3    _lastFieldPos, _lastFieldEuler;
    private Vector3    _lastTransformPos;
    private Quaternion _lastTransformRot;
    private bool       _init;

    void Update()
    {
        // Premiere frame : le Transform fait foi, les champs ne sont qu'un miroir
        // tant qu'aucun preset n'a ete rappele.
        if (!_init)
        {
            _init = true;
            PullFromTransform();
            return;
        }

        bool transformMoved = transform.localPosition != _lastTransformPos
                           || transform.localRotation != _lastTransformRot;

        bool fieldsChanged = position != _lastFieldPos
                          || euler    != _lastFieldEuler;

        if (transformMoved)     PullFromTransform();
        else if (fieldsChanged) PushToTransform();
    }

    /// <summary>Recopie la pose courante de l'objet dans les champs capturables.</summary>
    public void PullFromTransform()
    {
        position = transform.localPosition;
        euler    = transform.localEulerAngles;
        Remember();
    }

    /// <summary>Applique les champs a l'objet. Appele apres un rappel de preset.</summary>
    public void PushToTransform()
    {
        transform.localPosition    = position;
        transform.localEulerAngles = euler;
        Remember();
    }

    void Remember()
    {
        _lastTransformPos = transform.localPosition;
        _lastTransformRot = transform.localRotation;
        _lastFieldPos     = position;
        _lastFieldEuler   = euler;
    }
}

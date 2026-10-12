using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cablage du coup de pied, pour ne pas avoir a dimensionner des colliders a la
/// main sur un rig dont l'armature est a l'echelle x100 (un BoxCollider de
/// 0.001 en local y fait 18 cm a l'ecran).
///
/// Ordre d'utilisation :
///   1. selectionner "Laying BreathlessAvatar" > menu "1 - Configurer la cible"
///   2. selectionner "Onyx_New_Unity"          > menu "2 - Configurer le kicker"
///   3. si rien ne porte le corps sous la scene > menu "3 - Sol de collision"
///
/// "Diagnostic" relit la selection et dit ce qui manque.
/// </summary>
public static class CoupDePiedSetup
{
    const string MENU = "TOXXIC/Coup de pied/";

    // Boite de pied, en metres monde, exprimee dans les axes LOCAUX du bone.
    // Dans ce rig (export Blender) le +Y du bone pointe vers les orteils : dans
    // les donnees de squelette du FBX, Toes.L est a (0, +0.00133, 0) de Foot.L.
    // Donc Y = longueur du pied, X = largeur, Z = epaisseur.
    const float FOOT_BOX_X = 0.12f;  // largeur
    const float FOOT_BOX_Y = 0.28f;  // longueur, cheville > orteils
    const float FOOT_BOX_Z = 0.10f;  // epaisseur

    // Centre de detection de FootLauncher, en metres le long du pied : au milieu
    // de la semelle plutot que sur la cheville.
    const float FOOT_PROBE_FORWARD = 0.12f;
    const float FOOT_PROBE_RADIUS  = 0.18f;

    const string COLLIDER_CHILD = "KICK_COLLIDER";

    // ----------------------------------------------------------------- cible

    [MenuItem(MENU + "1 - Configurer la cible (selection)")]
    static void SetupTarget()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Warn("Selectionne le root de l'avatar a envoyer en l'air."); return; }

        var animator = go.GetComponent<Animator>();
        if (animator == null)
        {
            Warn("Pas d'Animator sur '" + go.name + "'. Selectionne le ROOT de l'avatar, " +
                 "pas le mesh : c'est le root qui doit porter le Rigidbody.");
            return;
        }

        Undo.SetCurrentGroupName("Configurer la cible du coup de pied");
        int group = Undo.GetCurrentGroup();
        var log = new StringBuilder();

        // 1. Menage. Un MeshCollider sans mesh n'a aucune geometrie de collision :
        //    il ne peut rien toucher. Et un Rigidbody sur un enfant anime ne
        //    deplace rien de visible, le mesh skinne suit ses bones.
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>(true))
        {
            if (mc.sharedMesh != null) continue;
            log.AppendLine("  - MeshCollider sans mesh retire sur '" + mc.gameObject.name + "'");
            Undo.DestroyObjectImmediate(mc);
        }

        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb.gameObject == go) continue;
            log.AppendLine("  - Rigidbody hors root retire sur '" + rb.gameObject.name + "'");
            Undo.DestroyObjectImmediate(rb);
        }

        // 2. Le Rigidbody va sur le root, celui qui porte l'Animator.
        var body = go.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = Undo.AddComponent<Rigidbody>(go);
            log.AppendLine("  + Rigidbody ajoute sur le root");
        }
        else Undo.RecordObject(body, "Rigidbody");

        body.mass                 = 20f;
        body.linearDamping        = 0.02f;
        body.angularDamping       = 0.05f;
        body.useGravity           = true;
        body.interpolation        = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.maxDepenetrationVelocity = 3f;

        if (animator.applyRootMotion)
        {
            Undo.RecordObject(animator, "Apply Root Motion");
            animator.applyRootMotion = false;
            log.AppendLine("  * Apply Root Motion coupe : sinon l'Animator reecrit la " +
                           "position du root et le corps ne decolle jamais");
        }

        // 3. La capsule, sur un enfant du root — donc hors de l'armature, l'anim
        //    ne la touche pas. Alignee tete > pieds pour un corps allonge.
        var capsule = BuildCapsule(go, animator, log);

        // 4. Le composant.
        var launchable = go.GetComponent<LaunchableBody>();
        if (launchable == null)
        {
            launchable = Undo.AddComponent<LaunchableBody>(go);
            log.AppendLine("  + LaunchableBody ajoute");
        }
        else Undo.RecordObject(launchable, "LaunchableBody");
        launchable.animator = animator;

        Undo.CollapseUndoOperations(group);
        EditorUtility.SetDirty(go);

        if (capsule != null)
            log.AppendLine("  = capsule " + capsule.height.ToString("0.00") + " m x r" +
                           capsule.radius.ToString("0.00") + " m sur '" + COLLIDER_CHILD + "'");

        log.AppendLine("  > physique DESARMEE au depart : le corps reste kinematic " +
                       "jusqu'au top. Branche 'physiqueLayingAvatar' de " +
                       "conduite_script03_Silk1 sur un bouton, ou coche 'armed' " +
                       "sur le LaunchableBody en Play pour essayer.");

        Report("Cible '" + go.name + "' configuree", log, go);
    }

    static CapsuleCollider BuildCapsule(GameObject root, Animator animator, StringBuilder log)
    {
        Transform child = root.transform.Find(COLLIDER_CHILD);
        if (child == null)
        {
            var created = new GameObject(COLLIDER_CHILD);
            Undo.RegisterCreatedObjectUndo(created, "Capsule de collision");
            child = created.transform;
            child.SetParent(root.transform, false);
            log.AppendLine("  + enfant '" + COLLIDER_CHILD + "' cree");
        }
        else Undo.RecordObject(child, "Capsule de collision");

        child.localScale = Vector3.one;

        var capsule = child.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = Undo.AddComponent<CapsuleCollider>(child.gameObject);
        else Undo.RecordObject(capsule, "Capsule de collision");

        // Axe du corps : tete > milieu des pieds. Les bounds des renderers ne
        // servent a rien ici, elles sont alignees sur les axes du monde.
        Transform head  = Bone(animator, HumanBodyBones.Head)  ?? Bone(animator, HumanBodyBones.Neck);
        Transform hips  = Bone(animator, HumanBodyBones.Hips);
        Transform footL = Bone(animator, HumanBodyBones.LeftFoot);
        Transform footR = Bone(animator, HumanBodyBones.RightFoot);

        Vector3 top, bottom;
        if (head != null && (footL != null || footR != null))
        {
            top = head.position;
            bottom = (footL != null && footR != null)
                   ? (footL.position + footR.position) * 0.5f
                   : (footL != null ? footL.position : footR.position);
        }
        else if (hips != null)
        {
            // Rig incomplet : capsule d'un metre autour des hanches.
            top    = hips.position + root.transform.up * 0.5f;
            bottom = hips.position - root.transform.up * 0.5f;
            log.AppendLine("  ! rig humanoide incomplet, capsule approximee autour des hanches");
        }
        else
        {
            log.AppendLine("  ! aucun bone humanoide trouve, capsule laissee telle quelle");
            return capsule;
        }

        float radiusWorld = 0.24f;
        Transform legL = Bone(animator, HumanBodyBones.LeftUpperLeg);
        Transform legR = Bone(animator, HumanBodyBones.RightUpperLeg);
        if (legL != null && legR != null)
            radiusWorld = Mathf.Clamp(Vector3.Distance(legL.position, legR.position) * 1.1f, 0.16f, 0.45f);

        Vector3 axis   = top - bottom;
        float   length = axis.magnitude;
        if (length < 0.01f) { log.AppendLine("  ! tete et pieds confondus, capsule inchangee"); return capsule; }

        Vector3 scale = root.transform.lossyScale;
        float   s     = (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f;
        if (s < 1e-4f) s = 1f;
        if (Mathf.Abs(scale.x - scale.z) > 0.01f * s || Mathf.Abs(scale.x - scale.y) > 0.01f * s)
            log.AppendLine("  ! echelle du root non uniforme (" + scale + "), la capsule sera approximative");

        Vector3 axisLocal = root.transform.InverseTransformDirection(axis.normalized).normalized;

        child.localPosition = root.transform.InverseTransformPoint((top + bottom) * 0.5f);
        child.localRotation = Quaternion.FromToRotation(Vector3.up, axisLocal);

        capsule.direction = 1; // Y, l'axe de la capsule apres la rotation ci-dessus
        capsule.center    = Vector3.zero;
        capsule.radius    = radiusWorld / s;
        capsule.height    = Mathf.Max(length, 2f * radiusWorld) / s;
        capsule.isTrigger = false;

        return capsule;
    }

    // ---------------------------------------------------------------- kicker

    [MenuItem(MENU + "2 - Configurer le kicker (selection)")]
    static void SetupKicker()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Warn("Selectionne le root de l'avatar qui frappe."); return; }

        var animator = go.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Warn("'" + go.name + "' n'a pas d'Animator humanoide. Selectionne le ROOT " +
                 "de l'avatar, ou renseigne les pieds a la main dans FootLauncher.");
            return;
        }

        Undo.SetCurrentGroupName("Configurer le kicker");
        int group = Undo.GetCurrentGroup();
        var log = new StringBuilder();

        var launcher = go.GetComponent<FootLauncher>();
        if (launcher == null)
        {
            launcher = Undo.AddComponent<FootLauncher>(go);
            log.AppendLine("  + FootLauncher ajoute");
        }
        else Undo.RecordObject(launcher, "FootLauncher");

        var feet = new List<Transform>(2);
        var l = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var r = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (l != null) feet.Add(l);
        if (r != null) feet.Add(r);

        if (feet.Count == 0) { Warn("Pas de bones LeftFoot/RightFoot dans le rig."); return; }

        launcher.feet       = feet.ToArray();
        launcher.footRadius = FOOT_PROBE_RADIUS;
        launcher.footOffset = new Vector3(0f, FOOT_PROBE_FORWARD, 0f);
        log.AppendLine("  = pieds : " + string.Join(", ", feet.ConvertAll(f => f.name).ToArray()));

        // Les colliders de pieds ne sont pas ce qui lance le corps — c'est
        // FootLauncher qui applique l'impulsion. Ils servent a ce que le corps
        // puisse aussi reposer sur le pied et se faire pousser au sol. Un
        // Rigidbody kinematic dessus, pour que PhysX les voie comme des corps
        // qui bougent et pas comme du decor qui se teleporte.
        foreach (var foot in feet)
        {
            var box = foot.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = Undo.AddComponent<BoxCollider>(foot.gameObject);
                log.AppendLine("  + BoxCollider ajoute sur '" + foot.name + "'");
            }
            else Undo.RecordObject(box, "BoxCollider pied");

            // L'armature est a l'echelle x100 dans le FBX, x1.8 sur le root : on
            // divise par l'echelle monde du bone pour que la boite fasse bien
            // les dimensions voulues a l'ecran.
            Vector3 fs = foot.lossyScale;
            box.size = new Vector3(
                FOOT_BOX_X / Mathf.Max(Mathf.Abs(fs.x), 1e-6f),
                FOOT_BOX_Y / Mathf.Max(Mathf.Abs(fs.y), 1e-6f),
                FOOT_BOX_Z / Mathf.Max(Mathf.Abs(fs.z), 1e-6f));

            // Decalee vers les orteils : le bone est a la cheville, le pied
            // s'etend devant lui.
            box.center = new Vector3(0f, FOOT_BOX_Y * 0.5f / Mathf.Max(Mathf.Abs(fs.y), 1e-6f), 0f);

            log.AppendLine("  = '" + foot.name + "' : box " + FOOT_BOX_X + " x " + FOOT_BOX_Y +
                           " x " + FOOT_BOX_Z + " m (echelle monde du bone : " + fs.x.ToString("0.#") + ")");

            var frb = foot.GetComponent<Rigidbody>();
            if (frb == null)
            {
                frb = Undo.AddComponent<Rigidbody>(foot.gameObject);
                log.AppendLine("  + Rigidbody kinematic ajoute sur '" + foot.name + "'");
            }
            else Undo.RecordObject(frb, "Rigidbody pied");
            frb.isKinematic = true;
            frb.useGravity  = false;
            frb.interpolation = RigidbodyInterpolation.None;
        }

        Undo.CollapseUndoOperations(group);
        EditorUtility.SetDirty(go);
        Report("Kicker '" + go.name + "' configure", log, go);
    }

    // ------------------------------------------------------------------- sol

    [MenuItem(MENU + "3 - Sol de collision sous la cible (selection)")]
    static void CreateFloor()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Warn("Selectionne la cible, le sol sera cree dessous."); return; }

        var floor = new GameObject("SOL_COLLISION_KICK");
        Undo.RegisterCreatedObjectUndo(floor, "Sol de collision");

        var box = floor.AddComponent<BoxCollider>();
        box.size = new Vector3(40f, 1f, 40f);

        Vector3 p = go.transform.position;
        floor.transform.position = new Vector3(p.x, p.y - 0.5f, p.z);
        floor.transform.SetParent(go.transform.parent, true);

        Selection.activeGameObject = floor;
        Debug.Log("[Coup de pied] Sol de collision cree a y=" + floor.transform.position.y.ToString("0.00") +
                  ". Invisible, 40x40 m. Ajuste sa hauteur pour qu'il colle au plancher du decor.", floor);
    }

    // ------------------------------------------------------------ diagnostic

    [MenuItem(MENU + "Diagnostic (selection)")]
    static void Diagnose()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Warn("Selectionne un objet a diagnostiquer."); return; }

        var log = new StringBuilder();

        for (Transform t = go.transform; t != null; t = t.parent)
            if (!t.gameObject.activeSelf)
                log.AppendLine("  ! '" + t.name + "' est DESACTIVE : en Play, rien de ce qui est " +
                               "dessous n'existe, ni les colliders ni les Rigidbody");

        foreach (var mc in go.GetComponentsInChildren<MeshCollider>(true))
            if (mc.sharedMesh == null)
                log.AppendLine("  ! MeshCollider sans mesh sur '" + mc.gameObject.name +
                               "' : aucune geometrie de collision, il ne touchera jamais rien");

        var bodies = go.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in bodies)
        {
            if (rb.gameObject != go)
                log.AppendLine("  ! Rigidbody sur l'enfant '" + rb.gameObject.name +
                               "' : s'il porte un SkinnedMeshRenderer, le deplacer ne change " +
                               "rien a l'image, le mesh suit ses bones");
            if (rb.gameObject == go && rb.isKinematic && go.GetComponent<LaunchableBody>() == null)
                log.AppendLine("  ! Rigidbody kinematic sans LaunchableBody : la physique ne le bougera pas");
        }

        int colliders = 0;
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            if (c.enabled && !(c is MeshCollider mc2 && mc2.sharedMesh == null)) colliders++;
        if (colliders == 0)
            log.AppendLine("  ! aucun collider utilisable sous '" + go.name + "'");

        var anim = go.GetComponent<Animator>();
        if (anim != null && anim.applyRootMotion && go.GetComponent<Rigidbody>() != null)
            log.AppendLine("  ! Apply Root Motion actif avec un Rigidbody : l'Animator reecrit " +
                           "la position du root a chaque frame, le corps ne decollera pas");

        if (bodies.Length > 0 && !Physics.Raycast(go.transform.position, Vector3.down, 200f))
            log.AppendLine("  ! rien sous '" + go.name + "' sur 200 m : le corps tombera dans le vide " +
                           "(menu '3 - Sol de collision')");

        if (log.Length == 0) log.AppendLine("  = rien a signaler.");
        Report("Diagnostic de '" + go.name + "'", log, go);
    }

    // ----------------------------------------------------------------- outils

    static Transform Bone(Animator a, HumanBodyBones b)
    {
        if (a == null || a.avatar == null || !a.avatar.isHuman) return null;
        return a.GetBoneTransform(b);
    }

    static void Warn(string msg)
    {
        Debug.LogWarning("[Coup de pied] " + msg);
    }

    static void Report(string title, StringBuilder log, Object context)
    {
        Debug.Log("[Coup de pied] " + title + "\n" + log.ToString().TrimEnd(), context);
    }
}

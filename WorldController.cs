using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
// using UnityEngine;

using ObjectId = System.Guid;

using ObjectData = System.Collections.Generic.Dictionary<System.Guid, Parameter>;
using ObjectIdPair = System.Collections.Generic.Dictionary<System.Guid, System.Guid>;
using MaskedObjectData = System.Collections.Generic.Dictionary<System.Collections.Generic.Dictionary<System.Guid, System.Guid>, MaskedParameter>;

using CredentialTable = System.Collections.Generic.Dictionary<System.Guid, string>;

public class WorldController : /*MonoBehaviour*/ GameObject {

    private ObjectData objectList = new ObjectData();  // オブジェクトの一覧
    private MaskedObjectData maskedObjectList = new MaskedObjectData();  // マスクされた情報の一覧
    private CredentialTable credentialTable = new CredentialTable();  // 認証情報

    // Use this for initialization
    public override void Start () {

    }
    
    // Update is called once per frame
    public override void Update () {
        foreach (var knowledge in objectList)
        {
            Console.WriteLine($"[{knowledge.Key.ToString()}](controller) object id: {knowledge.Key.ToString()}");
            Console.WriteLine($"[{knowledge.Key.ToString()}](controller) {knowledge.Value}");
        }
    }

    public static BasicObjectProgram Generator(WorldController controller) {
        var id = ObjectId.NewGuid();
        var metadata = new MetaData(null, null, 0, 0);
        var resources = new Resources(100);
        var eigenvalue = new Eigenvalue();
        var transform = new Transform();
        var parameter = new Parameter(metadata, eigenvalue, transform, resources);
        var maskedParam = new MaskedParameter(parameter, MaskedParameter.Mask.EIGENVALUE | MaskedParameter.Mask.TRANSFORM);
        var knowledge = new Knowledge(id, maskedParam);

        controller.credentialTable.Add(id, Credential.HashPassword(id.ToString(), Credential.HashAlgorithm.NOOP));

        var data = new BasicObjectProgram();
        data.controller = controller;
        data.knowledge = knowledge;

        controller.objectList.Add(id, parameter);

        var objectIdPair = new ObjectIdPair();
        objectIdPair.Add(id, id);
        controller.maskedObjectList.Add(objectIdPair, maskedParam);

        return data;
    }

    public static bool UpdateCredential(WorldController controller, ObjectId id, String oldPassword, String newPassword) {
        var hashedPassword = controller.credentialTable.GetValueOrDefault(id, "");
        if (Credential.VerifyHash(oldPassword, new UTF8Encoding().GetBytes(hashedPassword))) {
            controller.credentialTable.Remove(id);
            controller.credentialTable.Add(id, Credential.HashPassword(newPassword, Credential.HashAlgorithm.PBKDF2));
            return true;
        }
        return false;
    }

    public static bool Authorizer(WorldController controller, ObjectId id, String password) {
        var hashedPassword = controller.credentialTable.GetValueOrDefault(id, "");
        Console.WriteLine(Credential.VerifyHash(password, new UTF8Encoding().GetBytes(hashedPassword)));
        return Credential.VerifyHash(password, new UTF8Encoding().GetBytes(hashedPassword));
    }

    public static List<Knowledge> SearchNearObject(WorldController controller, ObjectId id, String authorityCode) {

        if (!WorldController.Authorizer(controller, id, authorityCode)) { return null; }

        var data = new System.Collections.Generic.Dictionary<System.Guid, MaskedParameter>();

        foreach(var item in controller.maskedObjectList)
        {
            foreach(var key in item.Key)
            {
                if (key.Key.Equals(id) && !key.Value.Equals(id))
                {
                    data.Add(key.Value, item.Value);
                }
            }
        }
        
        foreach(var item in controller.objectList)
        {
            if (item.Key.Equals(id)) { continue; }
            if (!data.ContainsKey(item.Key))
            {
                var t = new MaskedParameter(item.Value, MaskedParameter.Mask.NONE);
                data.Add(item.Key, t);
            }
        }

        var result = new List<Knowledge>();
        foreach(var item in data) {
            var t = new Knowledge(item.Key, item.Value);
            result.Add(t);
        }

        return result;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text.RegularExpressions;
using System.Text;
// using UnityEngine;

using ObjectId = System.Guid;

public class Credential {
    public String authorityCode;
    public enum HashAlgorithm {
        NOOP,
        PBKDF2
    }

    public Credential(String code) {
        this.authorityCode = code;
    }

    public static String HashPassword(String code, HashAlgorithm algo) {
        switch (algo) {
            case HashAlgorithm.NOOP:
                return HashPasswordNOOP(code.ToString());
            case HashAlgorithm.PBKDF2:
                return HashPasswordPBKDF2(code.ToString());
            default:
                return HashPasswordPBKDF2(code.ToString());
        }
    }

    public static String HashPasswordNOOP(String password) {
        return "{noop}" + password;
    }

    public static String HashPasswordPBKDF2(String password, int saltSize=128/8, int iterations=10000) {
        byte[] salt = new byte[saltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        byte[] subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: 256 / 8
        );

        var outputBytes = new byte[13 + salt.Length + subkey.Length];
        outputBytes[0] = 0x01;  // format maker
        WriteNetworkByteOrder(outputBytes,1, (uint)KeyDerivationPrf.HMACSHA256);
        WriteNetworkByteOrder(outputBytes, 5, (uint)iterations);
        WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
        Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
        Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);

        var hashed = Convert.ToBase64String(outputBytes);

        return "{pbkdf2}" + hashed;
    }

    public static bool VerifyHashPasswordPBKDF2(String password, byte[] hashedPassword) {
        KeyDerivationPrf prf = (KeyDerivationPrf)ReadNetworkByteOrder(hashedPassword, 1);
        var iterCount = (int)ReadNetworkByteOrder(hashedPassword, 5);
        int saltLength = (int)ReadNetworkByteOrder(hashedPassword, 9);

        if (saltLength < 128 / 8) {
            return false;
        }
        byte[] salt = new byte[saltLength];
        Buffer.BlockCopy(hashedPassword, 13, salt, 0, salt.Length);

        int subkeyLength = hashedPassword.Length - 13 - salt.Length;
        if (subkeyLength < 128 / 8) {
            return false;
        }
        byte[] expectedSubkey = new byte[subkeyLength];
        Buffer.BlockCopy(hashedPassword, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

        byte[] actualSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, subkeyLength);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    public static bool VerifyHash(byte[] hashedPassword1, byte[] hashedPassword2) {
        var a = new UTF8Encoding().GetString(hashedPassword1);
        var b = new UTF8Encoding().GetString(hashedPassword2);
        // return hashedPassword1.Equals(hashedPassword2);
        return a.Equals(b);
    }

    public static bool VerifyHash(String password, byte[] hashedPassword) {
        Match matche = Regex.Match(new UTF8Encoding().GetString(hashedPassword), "^{(noop)}|{(pbkdf2)}");
        var hashed = Regex.Match(new UTF8Encoding().GetString(hashedPassword), "^({noop}|{pbkdf2})(.*)").Groups[2].Value;
        switch (matche.Value) {
            case "{noop}":
                return VerifyHash(new UTF8Encoding().GetBytes(HashPasswordNOOP(password)), hashedPassword);
            case "{pbkdf2}":
                return VerifyHashPasswordPBKDF2(password, Convert.FromBase64String(hashed));
            default:
                return VerifyHashPasswordPBKDF2(password, Convert.FromBase64String(hashed));
        }
    }

    public static uint ReadNetworkByteOrder(byte[] buffer, int offset) {
        return ((uint)(buffer[offset + 0]) << 24)
            | ((uint)(buffer[offset + 1]) << 16)
            | ((uint)(buffer[offset + 2]) << 8)
            | ((uint)(buffer[offset + 3]));
    }

    public static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value) {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)(value >> 0);
    }
}

public class Knowledge {
    public ObjectId id;
    public MaskedParameter parameter;

    public Knowledge(ObjectId id, MaskedParameter parameter) {
        this.id = id;
        this.parameter = parameter;
    }

    public override string ToString() {
        return "Knowledge: (" + id.ToString() + ", " + parameter.ToString() + ")";
    }
}

// メタデータ
public class MetaData {
    public readonly Knowledge origin;
    public readonly Knowledge[] parents;
    public readonly uint created_at;
    public uint stopped_at;
    public uint authority;

    public MetaData(Knowledge origin, Knowledge[] parents, uint created_at, uint authority) {
        this.origin = origin;
        this.parents = parents;
        this.created_at = created_at;
        this.authority = authority;
    }

    public override string ToString() {
        var o = origin is null? "null": origin.ToString();
        var p = parents is null? "null": parents.ToString();
        return "MetaData: " + "\n  origin: " + o + "\n  parents: " + p + "\n  created_at: " + created_at + "\n  stopped_at: " + stopped_at + "\n  authority: " + authority;
    }
}

// 資源
public class Resources {
    // 物資
    public class Supplies {
        public Knowledge item;

        public override string ToString() {
            return "Supplies: " + item.ToString();
        }
    }
    // 体力
    public class Vitality {
        public int value;
        public Vitality(int value) {
            this.value = value;
        }

        public override string ToString() {
            return "Vitality: " + value;
        }
    }
    // 教養
    public class Education {
        public Knowledge knowledge;

        public override string ToString() {
            return "Education: " + knowledge.ToString();
        }
    }
    // 能力
    public interface Ability {
        bool Execute(WorldController controller, Knowledge a, Knowledge b);
    }

    public List<Supplies> supplies;
    public Vitality vital;
    public List<Education> education;
    public List<Ability> ability;

    public Resources(int vital) {
        this.supplies = new List<Supplies>();
        this.vital = new Vitality(vital);
        this.education = new List<Education>();
        this.ability = new List<Ability>();
    }

    public override string ToString() {
        return "Resources: \n" + "\n  supplies: " + supplies + "\n  " + vital + "\n  education: " + education + "\n  ability: " + ability;
    }
}

// 固有値
public class Eigenvalue {
    public Vector4 value;

    public Eigenvalue (Eigenvalue value) {
        this.value = value.value;
    }

    public Eigenvalue () {
        this.value = new Vector4();
    }

    public override string ToString() {
        return "Eigenvalue: (" + value.x + ", " + value.y + ", " + value.z + ", " + value.w + ")";
    }
}

// 基本パラメーター
public class Parameter {
    public MetaData metadata;
    public Eigenvalue eigenvalue;
    public Transform transform;
    public Resources resources;

    public Parameter(MetaData metadata, Eigenvalue eigenvalue, Transform transform, Resources resources) {
        this.metadata = metadata;
        this.eigenvalue = eigenvalue;
        this.transform = transform;
        this.resources = resources;
    }

    public Parameter(Parameter param) {
        this.metadata = param.metadata;
        this.eigenvalue = param.eigenvalue;
        this.transform = param.transform;
        this.resources = param.resources;
    }

    public Parameter() {
        this.metadata = null;
        this.eigenvalue = null;
        this.transform = null;
        this.resources = null;
    }

    public override string ToString() {
        return "Parameter: \n" + "\n  " + metadata.ToString() + "\n  " + eigenvalue.ToString() + "\n  " + transform.ToString() + "\n  " + resources.ToString();
    }
}

// マスクされたパラメーター情報
public class MaskedParameter : Parameter {
    public enum Mask {
        NONE = 0 << 0,
        META_DATA = 1 << 0,
        EIGENVALUE = 1 << 1,
        TRANSFORM = 1 << 2,
        RESOURCES = 1 << 3
    }

    public class MaskPermission {
        public Mask permit;
        public MaskPermission(Mask x) { permit = x; }
        public bool HasPermission(Mask x) { return ((permit&x)^x) == 0; }
    }

    public readonly MaskPermission permission;

    public MaskedParameter(Parameter param, Mask permit) {
        permission = new MaskPermission(permit);
        if (permission.HasPermission(Mask.META_DATA)) { this.metadata = param.metadata; }
        if (permission.HasPermission(Mask.EIGENVALUE)) { this.eigenvalue = param.eigenvalue; }
        if (permission.HasPermission(Mask.TRANSFORM)) { this.transform = param.transform; }
        if (permission.HasPermission(Mask.RESOURCES)) { this.resources = param.resources; }
    }

    public override string ToString() {
        var m = metadata is null? "MetaData: [masked]": metadata.ToString();
        var e = eigenvalue is null? "Eigenvalue: [masked]": eigenvalue.ToString();
        var t = transform is null? "Transform: [masked]": transform.ToString();
        var r = resources is null? "Resources: [masked]": resources.ToString();
        return "MaskedParameter: " + "\n  " + m + "\n  " + e + "\n  " + t + "\n  " + r;
    }
}

// このフレームでの作業予定タスク
public class Task {
    public class Plan {
        public Resources.Ability ability;
        public Knowledge from;
        public Knowledge to;

        public Plan (Resources.Ability ability, Knowledge from, Knowledge to) {
            this.ability = ability;
            this.from = from;
            this.to = to;
        }
    }
    public List<Plan> plans;

    public Task () {
        plans = new List<Plan>();
    }

    public int Add(Resources.Ability ability, Knowledge from, Knowledge to) {
        plans.Add(new Plan(ability, from, to));
        return plans.Count;
    }

    public int Execute(WorldController controller) {
        var successCount = 0;
        foreach (var item in plans)
        {
            if (item.ability.Execute(controller, item.from, item.to)) { successCount += 1; }
        }
        plans.Clear();

        return successCount;
    }
}

// 対象一覧とそれらに対する行動の意思決定
public class DecisionTree {
    public List<Knowledge> search(WorldController controller, ObjectId id, String authorityCode) {
        // コントローラーに周囲にいるオブジェクトの一覧を問い合わせる
        return WorldController.SearchNearObject(controller, id, authorityCode);
    }

    public int decide(WorldController controller, Knowledge param, List<Knowledge> objective, Task task) {
        // 周囲にいるオブジェクトと自分の知識から、それぞれに対する行動を決定する
        foreach (var item in objective)
        {
            var ability = judge(controller, param, item);
            task.Add(ability, param, item);
        }
        return objective.Count;
    }

    public Resources.Ability judge(WorldController controller, Knowledge param, Knowledge objective) {
        return new AbilityList.Remember();
    }
}

public class BasicObjectProgram : /*MonoBehaviour*/ GameObject {

    public WorldController controller;
    public Knowledge knowledge;
    private Credential credential;
    public DecisionTree decision;
    public Task task;

    // Use this for initialization
    public override void Start () {
        decision = new DecisionTree();
        task = new Task();
        credential = new Credential(Guid.NewGuid().ToString());
        WorldController.UpdateCredential(controller, knowledge.id, knowledge.id.ToString(), credential.authorityCode);

        Console.WriteLine($"[{knowledge.id.ToString()}] object id: {knowledge.id.ToString()}");
        Console.WriteLine($"[{knowledge.id.ToString()}] {knowledge.parameter}");
    }
    
    // Update is called once per frame
    public override void Update () {
        // 周囲の探索
        var objective = decision.search(controller, knowledge.id, credential.authorityCode);

        // 探索結果に対する行動の意思決定
        decision.decide(controller, knowledge, objective, task);
        Console.WriteLine($"[{knowledge.id.ToString()}] objective: {objective.Count}");

        // 行動の実施
        task.Execute(controller);
    }
}

// 能力リスト
public class AbilityList {
    // 自己複製
    // 物資を半減し、その他のパラメーターが同一の生命を生成する
    public class Duplication : Resources.Ability {
        public bool Execute(WorldController controller, Knowledge a, Knowledge b) {
            return true;
        }
    }

    // 学習
    // 対象から教養を取得
    public class Learn : Resources.Ability {
        public bool Execute(WorldController controller, Knowledge a, Knowledge b) {
            return true;
        }
    }

    // 記憶
    // 対象の固有値を取得する
    public class Remember : Resources.Ability {
        public bool Execute(WorldController controller, Knowledge a, Knowledge b) {
            Console.WriteLine($"[{a.id.ToString()}] execute: Remember");
            Console.WriteLine($"[{a.id.ToString()}] execute: from ({a.id})");
            Console.WriteLine($"[{a.id.ToString()}] execute: to ({b.id})");
            return true;
        }
    }

    // 共感
    // 対象に固有値を近づける
    public class Sympathize : Resources.Ability {
        public bool Execute(WorldController controller, Knowledge a, Knowledge b) {
            return true;
        }
    }
}

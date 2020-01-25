using System.Collections.Generic;

public abstract class GameObject {
    public virtual void Start(){}
    public virtual void Update(){}

    public GameObject() {
        GameObjectTask.objectList.Add(this);
    }
}

public class Transform {
    public double x;
    public double y;
    public double z;

    public Transform(double x, double y, double z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Transform() {
        this.x = 0;
        this.y = 0;
        this.z = 0;
    }

    public override string ToString() {
        return "Transform: (" + x + ", " + y + ", " + z + ")";
    }
}

public class Vector4 {
    public double x;
    public double y;
    public double z;
    public double w;

    public Vector4() {
        var r = new System.Random();
        x = r.NextDouble() * 2.0 - 1.0;
        y = r.NextDouble() * 2.0 - 1.0;
        z = r.NextDouble() * 2.0 - 1.0;
        w = r.NextDouble() * 2.0 - 1.0;
    }

    public Vector4(double x, double y, double z, double w) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public override string ToString() {
        return "Vector4: (" + x + ", " + y + ", " + z + ", " + w + ")";
    }
}

public class GameObjectTask {
    public static List<GameObject> objectList = new List<GameObject>();

    public static void Start() {
        foreach(var item in objectList)
        {
            item.Start();
        }
    }

    public static void Update() {
        foreach(var item in objectList)
        {
            item.Update();
        }
    }
}
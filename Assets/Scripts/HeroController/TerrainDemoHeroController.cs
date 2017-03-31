using UnityEngine;
using System.Collections;

public class TerrainDemoHeroController : MonoBehaviour
{
    public Camera Cam;
    public float HeroMoveSpeed;
    public Vector3 HeroFixPos=new Vector3(0,1,0);
    public Vector3 CameraFixPos = new Vector3(0, 7, 0);

    private Vector3 m_target_pos=new Vector3(0,1,0);

    private void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                m_target_pos=hit.point+ HeroFixPos;
            }
        }
        transform.position = Vector3.MoveTowards(transform.position, m_target_pos, HeroMoveSpeed * Time.deltaTime);
        transform.LookAt(m_target_pos);

        Cam.transform.position = transform.position + CameraFixPos;
        Cam.transform.LookAt(transform.position);
    }
}

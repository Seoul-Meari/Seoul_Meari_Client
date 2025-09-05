using UnityEngine;

public class PlaceInAir : MonoBehaviour
{
    public GameObject objectToPlace;  // 허공에 띄울 프리팹
    public float distanceFromCamera = 2f; // 카메라로부터 얼마나 앞에 띄울지 (미터 단위)

    void Start()
    {
        // 앱이 시작되자마자 허공에 객체 생성
        PlaceObject();
    }

    void PlaceObject()
    {
        // 메인 카메라를 찾음
        Transform cameraTransform = Camera.main.transform;

        // 카메라의 위치(position)와 바라보는 방향(forward)을 기준으로 생성 위치 계산
        Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

        // 계산된 위치에 프리apse 생성
        Instantiate(objectToPlace, spawnPosition, cameraTransform.rotation);
    }
}
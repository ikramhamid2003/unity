using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExplodeController : MonoBehaviour
{
    public Transform leftArm, rightArm, armor_part_1, armor_part_2, armor_part_3,
                     armor_part_4, armor_part_5, head, legs;
    public TMP_Text hintText;
    public ParticleSystem confettiEffect;
    public Slider progressSlider;
    public GameObject assembledText;
    public Transform robotRoot;
    public TMP_Text scoreText;
    public GameObject missionPanel;
    public TMP_Text statusText;
    public AudioSource completionAudio; // Drag the AudioSource here in Inspector
    public GameObject assemblyCompleteText; // Optional: UI text to show after completion


    private Vector3[] originalPositions;
    private Vector3[] explodedPositions;
    private bool[] isPartAssembled;
    private Transform[] interactableObjects;
    private bool isAssembled = false;

    public float moveSpeed = 2f;
    public float snapDistance = 0.3f;

    private Transform selectedObject = null;
    private Vector3 dragOffset;
    private Plane dragPlane;

    private Camera mainCamera;
    private bool isExploded = false;

    void Start()
    {
        mainCamera = Camera.main;

        interactableObjects = new Transform[] {
        leftArm, rightArm, armor_part_1, armor_part_2, armor_part_3,
        armor_part_4, armor_part_5, head, legs
    };

        originalPositions = new Vector3[interactableObjects.Length];
        explodedPositions = new Vector3[interactableObjects.Length];
        isPartAssembled = new bool[interactableObjects.Length];

        for (int i = 0; i < interactableObjects.Length; i++)
        {
            originalPositions[i] = interactableObjects[i].localPosition;
            explodedPositions[i] = originalPositions[i] + new Vector3((i - 4) * 0.4f, 0, 0);  // Explode X-axis
            isPartAssembled[i] = false;
        }

        StartCoroutine(ExplodeAfterDelay());  // Wait and explode
        if (hintText != null)
            StartCoroutine(FadeOutHint(3f));
        {
            // Existing setup...
            if (hintText != null)
                StartCoroutine(HideHintAfterDelay(3f));
        }

        IEnumerator HideHintAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            hintText.text = "";
        }
        {
            // Setup and explode
            if (missionPanel != null)
                missionPanel.SetActive(true);

            if (hintText != null)
                hintText.text = "Emergency detected! A rescue robot was damaged during a mission on Earth. Help reassemble it to resume its task of saving lives!";

            if (hintText != null)
                StartCoroutine(FadeOutHint(5f));  // Fades after 5 sec

            StartCoroutine(HideMissionPanelAfterDelay(5f));
        }
    }


    IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        ExplodeModel();
    }

    void ExplodeModel()
    {
        for (int i = 0; i < interactableObjects.Length; i++)
        {
            StartCoroutine(MoveToPosition(interactableObjects[i], explodedPositions[i]));
        }
        isExploded = true;
    }

    IEnumerator MoveToPosition(Transform obj, Vector3 targetPos)
    {
        float time = 0;
        Vector3 start = obj.localPosition;

        while (time < 1)
        {
            obj.localPosition = Vector3.Lerp(start, targetPos, time);
            time += Time.deltaTime * moveSpeed;
            yield return null;
        }

        obj.localPosition = targetPos;
    }
    IEnumerator HideMissionPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (missionPanel != null)
            missionPanel.SetActive(false);
    }



    void Update()
    {
        HandleTouch();
        if (!isAssembled && CheckIfRobotAssembled())
        {
            isAssembled = true;
            PlayCompletionSound();
        }
    }
    bool CheckIfRobotAssembled()
    {
        // Your custom logic to detect full assembly
        return true; // Replace this with real check
    }
    void PlayCompletionSound()
    {
        if (completionAudio != null)
        {
            completionAudio.Play();
        }

        if (assemblyCompleteText != null)
        {
            assemblyCompleteText.SetActive(true);
        }

        // Optional: Trigger robot animation
    }

    void HandleTouch()
    {
#if UNITY_EDITOR
        // Mouse Input (for Unity Editor)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (IsInteractable(hit.transform))
                {
                    selectedObject = hit.transform;

                    dragPlane = new Plane(mainCamera.transform.forward * -1, selectedObject.position);
                    if (dragPlane.Raycast(ray, out float enter))
                    {
                        Vector3 hitPoint = ray.GetPoint(enter);
                        dragOffset = selectedObject.position - hitPoint;
                    }
                }
            }
        }
        else if (Input.GetMouseButton(0) && selectedObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                selectedObject.position = Vector3.Lerp(selectedObject.position, hitPoint + dragOffset, 0.2f);
            }
        }
        else if (Input.GetMouseButtonUp(0) && selectedObject != null)
        {
            int index = System.Array.IndexOf(interactableObjects, selectedObject);
            float dist = Vector3.Distance(selectedObject.localPosition, originalPositions[index]);

            if (dist < snapDistance)
            {
                selectedObject.localPosition = originalPositions[index];
                isPartAssembled[index] = true;
            }
            else
            {
                isPartAssembled[index] = false;
            }

            selectedObject = null;
            UpdateProgress();
        }

else
    // Touchscreen Input (for mobile devices)
    if (Touchscreen.current == null || Touchscreen.current.touches.Count == 0) return;

    if (Touchscreen.current.touches.Count == 2)
    {
        Vector2 delta = Touchscreen.current.touches[0].delta.ReadValue();
        robotRoot.Rotate(0, -delta.x * 0.2f, 0);
        return;
    }

    var touch = Touchscreen.current.primaryTouch;

    if (touch.press.wasPressedThisFrame && selectedObject == null)
    {
        Ray ray = mainCamera.ScreenPointToRay(touch.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (IsInteractable(hit.transform))
            {
                selectedObject = hit.transform;
                dragPlane = new Plane(mainCamera.transform.forward * -1, selectedObject.position);
                float enter;
                dragPlane.Raycast(ray, out enter);
                Vector3 hitPoint = ray.GetPoint(enter);
                dragOffset = selectedObject.position - hitPoint;
            }
        }
    }

    if (touch.press.isPressed && selectedObject != null)
    {
        Ray ray = mainCamera.ScreenPointToRay(touch.position.ReadValue());
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            selectedObject.position = Vector3.Lerp(selectedObject.position, hitPoint + dragOffset, 0.2f);
        }
    }

    if (touch.press.wasReleasedThisFrame && selectedObject != null)
    {
        int index = System.Array.IndexOf(interactableObjects, selectedObject);
        float dist = Vector3.Distance(selectedObject.localPosition, originalPositions[index]);

        if (dist < snapDistance)
        {
            selectedObject.localPosition = originalPositions[index];
            isPartAssembled[index] = true;
        }
        else
        {
            isPartAssembled[index] = false;
        }

        selectedObject = null;
        UpdateProgress();
    }
#endif
    }

    void UpdateProgress()
    {
        int assembled = 0;
        foreach (bool b in isPartAssembled)
            if (b) assembled++;

        float progress = (float)assembled / interactableObjects.Length;

        if (progressSlider != null)
            progressSlider.value = progress;

        if (statusText != null)
        {
            if (progress < 1f)
            {
                if (assembled == 0)
                    statusText.text = "Begin assembling!";
                else if (assembled < interactableObjects.Length / 2)
                    statusText.text = "Good start! Keep assembling.";
                else if (assembled < interactableObjects.Length - 1)
                    statusText.text = "Only few parts left!";
                else
                    statusText.text = " I can almost feel my circuits coming alive!";
            }
            else
            {
                statusText.text = "Systems online. Thank you, Engineer. Mission can now resume!";
            }
        }

        if (assembledText != null)
            assembledText.SetActive(progress >= 1f);
        if (scoreText != null)
            scoreText.text = "Score: " + assembled + " / " + interactableObjects.Length;

        if (assembledText != null)
            assembledText.SetActive(progress >= 1f);
    }

    IEnumerator FadeOutHint(float delay)
    {
        yield return new WaitForSeconds(delay);

        float fadeDuration = 1f;
        float elapsedTime = 0f;
        Color originalColor = hintText.color;

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            hintText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        hintText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

    bool IsInteractable(Transform t)
    {
        return System.Array.IndexOf(interactableObjects, t) >= 0;
    }
}

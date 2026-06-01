using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BookshelfBookPuzzle : MonoBehaviour
{
    [SerializeField] private BookshelfBook[] books = new BookshelfBook[3];
    [SerializeField] private int[] correctOrder = { 0, 1, 2 };
    [SerializeField] private GameObject doorToDestroy;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField, Min(0.1f)] private float maxClickDistance = 100f;
    [SerializeField] private Vector3 pushedLocalOffset = new Vector3(0f, 0f, -0.15f);
    [SerializeField, Min(0.01f)] private float pushDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float resetDuration = 0.16f;

    private Vector3[] defaultLocalPositions;
    private Coroutine[] movementRoutines;
    private bool[] pushedBooks;
    private int currentOrderIndex;
    private bool solved;
    private bool inputLocked;

    private void OnValidate()
    {
        ConfigureBooks();
    }

    private void Awake()
    {
        FindBooksIfNeeded();
        ConfigureBooks();
        interactionCamera = interactionCamera != null ? interactionCamera : Camera.main;

        defaultLocalPositions = new Vector3[books.Length];
        movementRoutines = new Coroutine[books.Length];
        pushedBooks = new bool[books.Length];

        for (int i = 0; i < books.Length; i++)
        {
            if (books[i] != null)
            {
                defaultLocalPositions[i] = books[i].transform.localPosition;
            }
        }
    }

    private void Update()
    {
        if (!WasClickPressed())
        {
            return;
        }

        if (interactionCamera == null)
        {
            Debug.LogWarning("BookshelfBookPuzzle needs a camera.");
            return;
        }

        BookshelfBook clickedBook = GetClickedBook();
        if (clickedBook != null)
        {
            PressBook(clickedBook.bookIndex);
        }
    }

    private void FindBooksIfNeeded()
    {
        bool missingBook = books == null || books.Length == 0;

        if (!missingBook)
        {
            for (int i = 0; i < books.Length; i++)
            {
                if (books[i] == null)
                {
                    missingBook = true;
                    break;
                }
            }
        }

        if (!missingBook)
        {
            return;
        }

        books = FindObjectsByType<BookshelfBook>(FindObjectsSortMode.None);
        Array.Sort(books, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
    }

    private void ConfigureBooks()
    {
        if (books == null)
        {
            return;
        }

        for (int i = 0; i < books.Length; i++)
        {
            if (books[i] != null)
            {
                books[i].bookIndex = i;
            }
        }
    }

    public void PressBook(int bookIndex)
    {
        if (solved || inputLocked || !IsValidBook(bookIndex) || pushedBooks[bookIndex])
        {
            return;
        }

        if (!IsNextCorrectBook(bookIndex))
        {
            StartCoroutine(HandleWrongAttempt(bookIndex));
            return;
        }

        currentOrderIndex++;
        SetBookPushed(bookIndex, true, pushDuration);

        if (currentOrderIndex >= correctOrder.Length)
        {
            SolvePuzzle();
        }
    }

    private IEnumerator HandleWrongAttempt(int wrongBookIndex)
    {
        inputLocked = true;
        SetBookPushed(wrongBookIndex, true, pushDuration);

        yield return new WaitForSeconds(pushDuration);

        currentOrderIndex = 0;
        ResetBooks();

        yield return new WaitForSeconds(resetDuration);

        inputLocked = false;
    }

    private void SolvePuzzle()
    {
        solved = true;
        currentOrderIndex = correctOrder.Length;

        for (int i = 0; i < books.Length; i++)
        {
            if (books[i] != null)
            {
                SetBookPushed(i, true, pushDuration);
            }
        }

        if (doorToDestroy != null)
        {
            Destroy(doorToDestroy);
        }
    }

    private void ResetBooks()
    {
        for (int i = 0; i < books.Length; i++)
        {
            if (books[i] != null)
            {
                SetBookPushed(i, false, resetDuration);
            }
        }
    }

    private bool IsNextCorrectBook(int bookIndex)
    {
        return currentOrderIndex < correctOrder.Length
            && correctOrder[currentOrderIndex] == bookIndex;
    }

    private bool IsValidBook(int bookIndex)
    {
        return bookIndex >= 0
            && bookIndex < books.Length
            && books[bookIndex] != null
            && correctOrder != null
            && correctOrder.Length > 0;
    }

    private void SetBookPushed(int bookIndex, bool pushed, float duration)
    {
        pushedBooks[bookIndex] = pushed;

        if (movementRoutines[bookIndex] != null)
        {
            StopCoroutine(movementRoutines[bookIndex]);
        }

        Vector3 targetPosition = defaultLocalPositions[bookIndex] + (pushed ? pushedLocalOffset : Vector3.zero);
        movementRoutines[bookIndex] = StartCoroutine(MoveBook(books[bookIndex].transform, targetPosition, duration, bookIndex));
    }

    private BookshelfBook GetClickedBook()
    {
        Ray ray = interactionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, maxClickDistance, clickableLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            BookshelfBook book = hits[i].collider.GetComponentInParent<BookshelfBook>();
            if (book != null)
            {
                return book;
            }
        }

        return null;
    }

    private IEnumerator MoveBook(Transform book, Vector3 targetLocalPosition, float duration, int bookIndex)
    {
        Vector3 startPosition = book.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            book.localPosition = Vector3.Lerp(startPosition, targetLocalPosition, t);
            yield return null;
        }

        book.localPosition = targetLocalPosition;
        movementRoutines[bookIndex] = null;
    }

    private bool WasClickPressed()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<T>
{
    private List<(T Item, float Priority)> heap = new List<(T Item, float Priority)>();
    private Dictionary<T, int> itemIndexMap = new();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        int index = heap.Count - 1;
        itemIndexMap[item] = index;
        HeapifyUp(index);
    }

    public float PeekPriority(T item)
    {
        if (heap.Count == 0) { throw new InvalidOperationException("PriorityQueue is empty"); }

        return itemIndexMap[item];
    }

    public (T Item, float Priority) Dequeue()
    {
        if (heap.Count == 0) { throw new InvalidOperationException("PriorityQueue is empty"); }

        (T Item, float Priority) item = heap[0];
        Swap(0, heap.Count - 1);
        heap.RemoveAt(heap.Count - 1);
        itemIndexMap.Remove(item.Item);
        HeapifyDown(0);

        return item;
    }

    public void UpdatePriority(T item, float newPriority)
    {
        if (!itemIndexMap.TryGetValue(item, out int index)) { return; }

        float oldPriority = heap[index].Priority;
        heap[index] = (item, newPriority);

        if (newPriority < oldPriority) { HeapifyUp(index); }
        else { HeapifyDown(index); }
    }

    public bool Contains(T item) => itemIndexMap.ContainsKey(item);

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[index].Priority >= heap[parent].Priority) { break; }
            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int count = heap.Count;

        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left < count && heap[left].Priority < heap[smallest].Priority) { smallest = left; }
            if (right < count && heap[right].Priority < heap[smallest].Priority) { smallest = right; }
            if (smallest == index) { break; }

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int i, int j)
    {
        (heap[i], heap[j]) = (heap[j], heap[i]);
        itemIndexMap[heap[i].Item] = i;
        itemIndexMap[heap[j].Item] = j;
    }
}

using Luthetus.TextEditor.RazorLib.Characters.Models;
using Luthetus.TextEditor.RazorLib.Exceptions;

namespace Luthetus.TextEditor.RazorLib.TextEditors.Models;

public partial class TextEditorModelModifier : ITextEditorModel
{
    public void __Insert(int globalPositionIndex, RichCharacter richCharacter)
    {
        int indexOfPartitionWithAvailableSpace = -1;
        int relativePositionIndex = -1;
        var runningCount = 0;

        for (int i = 0; i < _partitionList.Count; i++)
        {
            TextEditorPartition? partition = _partitionList[i];

            if (runningCount + partition.Count >= globalPositionIndex)
            {
                // This is the partition we want to modify.
                // But, we must first check if it has available space.
                if (partition.Count >= PartitionSize)
                {
                    __SplitIntoTwoPartitions(i);
                    i--;
                    continue;
                }

                relativePositionIndex = globalPositionIndex - runningCount;
                indexOfPartitionWithAvailableSpace = i;
                break;
            }
            else
            {
                runningCount += partition.Count;
            }
        }

        if (indexOfPartitionWithAvailableSpace == -1)
            throw new LuthetusTextEditorException("if (indexOfPartitionWithAvailableSpace == -1)");

        if (relativePositionIndex == -1)
            throw new LuthetusTextEditorException("if (relativePositionIndex == -1)");

        var inPartition = _partitionList[indexOfPartitionWithAvailableSpace];
        var outPartition = inPartition.Insert(relativePositionIndex, richCharacter);

        _partitionList = _partitionList.SetItem(
            indexOfPartitionWithAvailableSpace,
            outPartition);
    }

    /// <summary>
    /// If either character or decoration byte are 'null', then the respective
    /// collection will be left unchanged.
    /// 
    /// i.e.: to change ONLY a character value invoke this method with decorationByte set to null,
    ///       and only the <see cref="CharList"/> will be changed.
    /// </summary>
    public void __SetItem(
        int globalPositionIndex,
        RichCharacter richCharacter)
    {
        int indexOfPartitionWithAvailableSpace = -1;
        int relativePositionIndex = -1;
        var runningCount = 0;

        for (int i = 0; i < _partitionList.Count; i++)
        {
            TextEditorPartition? partition = _partitionList[i];

            if (runningCount + partition.Count > globalPositionIndex)
            {
                // This is the partition we want to modify.
                relativePositionIndex = globalPositionIndex - runningCount;
                indexOfPartitionWithAvailableSpace = i;
                break;
            }
            else
            {
                runningCount += partition.Count;
            }
        }

        if (indexOfPartitionWithAvailableSpace == -1)
            throw new LuthetusTextEditorException("if (indexOfPartitionWithAvailableSpace == -1)");

        if (relativePositionIndex == -1)
            throw new LuthetusTextEditorException("if (relativePositionIndex == -1)");

        var inPartition = _partitionList[indexOfPartitionWithAvailableSpace];
        var outPartition = inPartition.SetItem(relativePositionIndex, richCharacter);

        _partitionList = _partitionList.SetItem(
            indexOfPartitionWithAvailableSpace,
            outPartition);
    }

    public void __SetDecorationByte(
        int globalPositionIndex,
        byte decorationByte)
    {
        int indexOfPartitionWithAvailableSpace = -1;
        int relativePositionIndex = -1;
        var runningCount = 0;

        for (int i = 0; i < _partitionList.Count; i++)
        {
            TextEditorPartition? partition = _partitionList[i];

            if (runningCount + partition.Count > globalPositionIndex)
            {
                // This is the partition we want to modify.
                relativePositionIndex = globalPositionIndex - runningCount;
                indexOfPartitionWithAvailableSpace = i;
                break;
            }
            else
            {
                runningCount += partition.Count;
            }
        }

        if (indexOfPartitionWithAvailableSpace == -1)
            throw new LuthetusTextEditorException("if (indexOfPartitionWithAvailableSpace == -1)");

        if (relativePositionIndex == -1)
            throw new LuthetusTextEditorException("if (relativePositionIndex == -1)");

        var inPartition = _partitionList[indexOfPartitionWithAvailableSpace];
        var targetRichCharacter = inPartition.RichCharacterList[relativePositionIndex];
        
        inPartition.RichCharacterList[relativePositionIndex] = new(
        	targetRichCharacter.Value,
        	decorationByte);
    }

    public void __RemoveAt(int globalPositionIndex)
    {
        if (globalPositionIndex >= CharCount)
            return;

        int indexOfPartitionWithContent = -1;
        int relativePositionIndex = -1;
        var runningCount = 0;

        for (int i = 0; i < _partitionList.Count; i++)
        {
            TextEditorPartition? partition = _partitionList[i];

            if (runningCount + partition.Count > globalPositionIndex)
            {
                // This is the partition we want to modify.
                relativePositionIndex = globalPositionIndex - runningCount;
                indexOfPartitionWithContent = i;
                break;
            }
            else
            {
                runningCount += partition.Count;
            }
        }

        if (indexOfPartitionWithContent == -1)
            throw new LuthetusTextEditorException("if (indexOfPartitionWithContent == -1)");

        if (relativePositionIndex == -1)
            throw new LuthetusTextEditorException("if (relativePositionIndex == -1)");

        var inPartition = _partitionList[indexOfPartitionWithContent];
        var outPartition = inPartition.RemoveAt(relativePositionIndex);

        _partitionList = _partitionList.SetItem(
            indexOfPartitionWithContent,
            outPartition);
    }

    private void __InsertNewPartition(int partitionIndex)
    {
        _partitionList = _partitionList.Insert(partitionIndex, new TextEditorPartition(new List<RichCharacter>()));
    }

    private void __SplitIntoTwoPartitions(int partitionIndex)
    {
        var originalPartition = _partitionList[partitionIndex];

        var firstUnevenSplit = PartitionSize / 2 + PartitionSize % 2;
        var secondUnevenSplit = PartitionSize / 2;

        // Validate multi-byte characters go on same partition (i.e.: '\r\n')
        {
            // firstUnevenSplit is a count so -1 to make it an index
            if (originalPartition.RichCharacterList[firstUnevenSplit - 1].Value == '\r')
            {
                if (originalPartition.RichCharacterList[(firstUnevenSplit - 1) + 1].Value == '\n')
                {
                    firstUnevenSplit += 1;
                    secondUnevenSplit -= 1;
                }
            }

            // TODO: If the partition to split ends in '\r' and the cause for the split
            //       is to create space in order to insert a '\n',
            //       |
            //       Then this works out as a "happy accident" of sorts.
            //       This is not ideal, it should be more concrete than "oops it worked".
            //       |
            //       The reason it works is because a split won't check if the next partition
            //       has space (? source needed) and will always move the '\r' to the new partition,
            //       then return to the insert and put the '\n' immediately after.

            // One of the reasons for not having a multi-byte character span multiple partitions,
            // is that if a partition has capacity 4,096 but a count of 2,048,
            // one cannot insert between the bytes of a multi-byte character
            // so the first partition with only half its capacity used, would be unable to be used
            // any further than 2,048 because it would mean writing between its multi-byte character
            // than spans into the next partition.
        }

        // Replace old
        {
            var partition = new TextEditorPartition(originalPartition.RichCharacterList
                .Skip(0)
                .Take(firstUnevenSplit)
                .ToList());

            _partitionList = _partitionList.SetItem(
                partitionIndex,
                partition);
        }

        // Insert new
        {
            var partition = new TextEditorPartition(originalPartition.RichCharacterList
                .Skip(firstUnevenSplit)
                .Take(secondUnevenSplit)
                .ToList());

            _partitionList = _partitionList.Insert(
                partitionIndex + 1,
                partition);
        }
    }

    public void __InsertRange(int globalPositionIndex, IEnumerable<RichCharacter> richCharacterList)
    {
        var richCharacterEnumerator = richCharacterList.GetEnumerator();

        while (richCharacterEnumerator.MoveNext())
        {
            int indexOfPartitionWithAvailableSpace = -1;
            int relativePositionIndex = -1;
            var runningCount = 0;
            TextEditorPartition? partition;

            for (int i = 0; i < _partitionList.Count; i++)
            {
                partition = _partitionList[i];

                if (runningCount + partition.Count >= globalPositionIndex)
                {
                    if (partition.Count >= PartitionSize)
                    {
                        __SplitIntoTwoPartitions(i);
                        i--;
                        continue;
                    }

                    relativePositionIndex = globalPositionIndex - runningCount;
                    indexOfPartitionWithAvailableSpace = i;
                    break;
                }
                else
                {
                    runningCount += partition.Count;
                }
            }

            if (indexOfPartitionWithAvailableSpace == -1)
                throw new LuthetusTextEditorException("if (indexOfPartitionWithAvailableSpace == -1)");

            if (relativePositionIndex == -1)
                throw new LuthetusTextEditorException("if (relativePositionIndex == -1)");

            partition = _partitionList[indexOfPartitionWithAvailableSpace];
            var partitionAvailableSpace = PartitionSize - partition.Count;

            var richCharacterBatchInsertList = new List<RichCharacter> { richCharacterEnumerator.Current };

            while (richCharacterBatchInsertList.Count < partitionAvailableSpace && richCharacterEnumerator.MoveNext())
            {
                richCharacterBatchInsertList.Add(richCharacterEnumerator.Current);
            }

            var inPartition = _partitionList[indexOfPartitionWithAvailableSpace];
            var outPartition = inPartition.InsertRange(relativePositionIndex, richCharacterBatchInsertList);

            _partitionList = _partitionList.SetItem(
                indexOfPartitionWithAvailableSpace,
                outPartition);

            globalPositionIndex += richCharacterBatchInsertList.Count;
        }
    }

    /// <summary>
    /// This method modifies the <see cref="TextEditorPartition"/>.
    /// The method only understands singular char values, 
    /// as opposed to the text editor which interprets "\r\n" as a single character,
    /// while encompassing 2 'char' values.<br/><br/>
    /// 
    /// One needs to be cautious with this method. The line ending: "\r\n"
    /// (or any other '2 char' long character),
    /// one can remove 1 of the two characters, and the other will still remain.<br/><br/>
    /// 
    /// If the text editor tells this method to remove "\r\n",
    /// then that is a count of 2 here. Even though for the text
    /// editor, it would describe "\r\n" as a count of 1.
    /// </summary>
    public void __RemoveRange(int argStartGlobalPositionIndex, int argTargetDeleteCount)
    {
        var sumGlobalDeleteCount = 0;
        var sumPartitionCount = 0;
        var globalPositionIndex = argStartGlobalPositionIndex;

        if (globalPositionIndex >= CharCount)
            return;

        for (int i = 0; i < _partitionList.Count; i++)
        {
            var partition = _partitionList[i];

            // TODO: '>' or '>=' ... If the partition.Count == 0, this if statement won't be entered.
            if (sumPartitionCount + partition.Count > globalPositionIndex)
            {
                var relativePositionIndex = globalPositionIndex - sumPartitionCount;
                
                var ableToDeleteCount = partition.RichCharacterList.Count - relativePositionIndex;

                var relativeDeleteCount = ableToDeleteCount < argTargetDeleteCount
                    ? ableToDeleteCount
                    : argTargetDeleteCount;

                globalPositionIndex += relativeDeleteCount;
                sumGlobalDeleteCount += relativeDeleteCount;

                // The "combining" of fragmented partitions into a single partition isn't being done within
                // this method, so the indices should be fine.
                _partitionList = _partitionList.SetItem(
                    i,
                    partition.RemoveRange(relativePositionIndex, relativeDeleteCount));
            }
                
            sumPartitionCount += partition.Count;

            if (sumGlobalDeleteCount >= argTargetDeleteCount)
                return;
        }
    }

    public void __Add(RichCharacter richCharacter)
    {
        __Insert(CharCount, richCharacter);
    }
}


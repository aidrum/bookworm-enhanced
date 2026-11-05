using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Sexy
{
    /// <summary>
    /// Saving and loading a game in progress (0x5908 / 0x4c90). The original fwrites raw struct fields (format
    /// version 0xd); the port writes its own format instead: the random generator, the grid (tile positions
    /// relative to their cells, as the original does, so a save survives a layout change), the selection, then
    /// the plain fields of the Board, WordScore and the two StatKeepers keyed by name, so fields ported later
    /// are saved too and old saves still load.
    /// </summary>
    public partial class Board
    {
        private const int SAVE_VERSION = 1;

        private const BindingFlags SAVE_FIELDS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        /// <summary>Board fields that are not game state: the dirty flag, the running clock, pause reasons,
        /// layout sizes, the touch in progress and the open hint (dialogs are not restored).</summary>
        private static readonly HashSet<string> SAVE_SKIP = new HashSet<string>
        {
            "mUpdated", "mClockStart", "mPauseFlags", "mTileWidth", "mTileHeight", "mCols", "mRows",
            "mLastHitFrame", "mTouchMode", "mHintShowing", "mPiecesMoving", "mSubmitWaitCount", "mSubmitArrow",
            "mSubmitArrowX", "mSubmitArrowY", "mSubmitArrowAlpha"
        };

        private const byte FIELD_INT = 0;
        private const byte FIELD_DOUBLE = 1;
        private const byte FIELD_BOOL = 2;
        private const byte FIELD_CHAR = 3;
        private const byte FIELD_STRING = 4;
        private const byte FIELD_INTS = 5;
        private const byte FIELD_PIECE = 6;

        /// <summary>
        /// FUN_00005908: saves the game of theMode (classic.dat / action.dat). Effects, embers, the overlay and
        /// Lex's animation are cleared first and the score roll-up finishes, as in the original; the play clock
        /// is banked. Also saves the personal bests. Also saves the collections (FUN_0002ba78).
        /// </summary>
        public bool SaveGame(int theMode)
        {
            mEffects?.Clear();
            mEmbers?.Clear();
            mOverlay?.mEffects?.Clear();
            mApp.mLex?.ResetAnim();
            mDisplayScore = mGameStats.mScore;
            int aNow = Environment.TickCount;
            if (mPauseFlags == 0)
            {
                mPlaySeconds += (aNow - mClockStart) / 1000;
                mClockStart = aNow;
            }
            mApp.mCollectionator.Save();
            mApp.mPersonalBests.Save();
            if (theMode != BookWormApp.GAME_CLASSIC && theMode != BookWormApp.GAME_ARCADE)
            {
                return false;
            }
            try
            {
                using (BinaryWriter aWriter = new BinaryWriter(File.Create(mApp.GetSavedGamePath(theMode))))
                {
                    aWriter.Write(SAVE_VERSION);
                    for (int i = 0; i < mRand.mt.Length; i++)
                    {
                        aWriter.Write(mRand.mt[i]);
                    }
                    aWriter.Write(mRand.mti);
                    for (int aRow = 0; aRow < ROWS; aRow++)
                    {
                        for (int aCol = 0; aCol < COLS; aCol++)
                        {
                            Piece aPiece = mGrid[aRow, aCol];
                            aWriter.Write(aPiece != null);
                            if (aPiece != null)
                            {
                                aWriter.Write(aPiece.mX - GetColX(aCol));
                                aWriter.Write(aPiece.mY - GetRowY(aCol, aRow));
                                WriteFields(aWriter, aPiece, null);
                            }
                        }
                    }
                    aWriter.Write(mSelection.Count);
                    foreach (Piece aPiece in mSelection)
                    {
                        aWriter.Write(GetPieceIndex(aPiece));
                    }
                    WriteFields(aWriter, this, SAVE_SKIP);
                    WriteFields(aWriter, mWordScore, null);
                    WriteFields(aWriter, mGameStats, null);
                    WriteFields(aWriter, mLevelStats, null);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return false;
            }
            mUpdated = false;
            return true;
        }

        /// <summary>
        /// 0x4c90: loads the saved game of theMode into this new board. (The original also sets the unread burn speed global
        /// 0x10c650 from the level, see ARCADE_BURN_DELAYS.)
        /// </summary>
        public bool LoadGame(int theMode)
        {
            mUpdated = false;
            if (theMode != BookWormApp.GAME_CLASSIC && theMode != BookWormApp.GAME_ARCADE)
            {
                return false;
            }
            try
            {
                using (BinaryReader aReader = new BinaryReader(File.OpenRead(mApp.GetSavedGamePath(theMode))))
                {
                    if (aReader.ReadInt32() > SAVE_VERSION)
                    {
                        return false;
                    }
                    for (int i = 0; i < mRand.mt.Length; i++)
                    {
                        mRand.mt[i] = aReader.ReadUInt32();
                    }
                    mRand.mti = aReader.ReadInt32();
                    for (int aRow = 0; aRow < ROWS; aRow++)
                    {
                        for (int aCol = 0; aCol < COLS; aCol++)
                        {
                            Piece aPiece = null;
                            if (aReader.ReadBoolean())
                            {
                                double aX = aReader.ReadDouble();
                                double aY = aReader.ReadDouble();
                                aPiece = new Piece();
                                ReadFields(aReader, aPiece);
                                aPiece.mX = aX + GetColX(aCol);
                                aPiece.mY = aY + GetRowY(aCol, aRow);
                            }
                            mGrid[aRow, aCol] = aPiece;
                        }
                    }
                    mSelection.Clear();
                    for (int aCount = aReader.ReadInt32(); aCount > 0; aCount--)
                    {
                        Piece aPiece = GetPieceAt(aReader.ReadInt32());
                        if (aPiece != null)
                        {
                            mSelection.Add(aPiece);
                        }
                    }
                    ReadFields(aReader, this);
                    ReadFields(aReader, mWordScore);
                    ReadFields(aReader, mGameStats);
                    ReadFields(aReader, mLevelStats);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is EndOfStreamException)
            {
                return false;
            }
            mClockStart = Environment.TickCount;
            // The touch is not saved: a restored selection was released, so the next tap must follow it
            // (left at 0, "picking", any tile would add to the word).
            mTouchMode = mSelection.Count > 0 ? 1 : 0;
            return true;
        }

        /// <summary>FUN_00004c00: there is something to save: the board has run and the game is not over.</summary>
        public bool CanSaveGame()
        {
            return mUpdated && mState != STATE_GAME_OVER;
        }

        private int GetPieceIndex(Piece thePiece)
        {
            for (int aRow = 0; aRow < ROWS; aRow++)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    if (thePiece != null && mGrid[aRow, aCol] == thePiece)
                    {
                        return aRow * COLS + aCol;
                    }
                }
            }
            return -1;
        }

        private Piece GetPieceAt(int theIndex)
        {
            return theIndex >= 0 && theIndex < ROWS * COLS ? mGrid[theIndex / COLS, theIndex % COLS] : null;
        }

        /// <summary>Writes theObject's int/double/bool/char/string/int[]/Piece fields as (name, type, value).</summary>
        private void WriteFields(BinaryWriter theWriter, object theObject, HashSet<string> theSkip)
        {
            List<FieldInfo> aFields = new List<FieldInfo>();
            foreach (FieldInfo aField in theObject.GetType().GetFields(SAVE_FIELDS))
            {
                if (!aField.IsInitOnly && GetFieldKind(aField.FieldType) >= 0 && (theSkip == null || !theSkip.Contains(aField.Name)))
                {
                    aFields.Add(aField);
                }
            }
            theWriter.Write(aFields.Count);
            foreach (FieldInfo aField in aFields)
            {
                int aKind = GetFieldKind(aField.FieldType);
                object aValue = aField.GetValue(theObject);
                theWriter.Write(aField.Name);
                theWriter.Write((byte)aKind);
                switch (aKind)
                {
                case FIELD_INT: theWriter.Write((int)aValue); break;
                case FIELD_DOUBLE: theWriter.Write((double)aValue); break;
                case FIELD_BOOL: theWriter.Write((bool)aValue); break;
                case FIELD_CHAR: theWriter.Write((ushort)(char)aValue); break;
                case FIELD_STRING: theWriter.Write((string)aValue ?? ""); break;
                case FIELD_PIECE: theWriter.Write(GetPieceIndex((Piece)aValue)); break;
                case FIELD_INTS:
                    int[] anInts = (int[])aValue ?? new int[0];
                    theWriter.Write(anInts.Length);
                    foreach (int anInt in anInts)
                    {
                        theWriter.Write(anInt);
                    }
                    break;
                }
            }
        }

        /// <summary>Reads what <see cref="WriteFields"/> wrote; unknown names or changed types are skipped.</summary>
        private void ReadFields(BinaryReader theReader, object theObject)
        {
            Type aType = theObject.GetType();
            for (int aCount = theReader.ReadInt32(); aCount > 0; aCount--)
            {
                string aName = theReader.ReadString();
                int aKind = theReader.ReadByte();
                object aValue;
                switch (aKind)
                {
                case FIELD_INT: aValue = theReader.ReadInt32(); break;
                case FIELD_DOUBLE: aValue = theReader.ReadDouble(); break;
                case FIELD_BOOL: aValue = theReader.ReadBoolean(); break;
                case FIELD_CHAR: aValue = (char)theReader.ReadUInt16(); break;
                case FIELD_STRING: aValue = theReader.ReadString(); break;
                case FIELD_PIECE: aValue = GetPieceAt(theReader.ReadInt32()); break;
                case FIELD_INTS:
                    int[] anInts = new int[theReader.ReadInt32()];
                    for (int i = 0; i < anInts.Length; i++)
                    {
                        anInts[i] = theReader.ReadInt32();
                    }
                    aValue = anInts;
                    break;
                default:
                    throw new IOException("Bad saved game field " + aName);
                }
                FieldInfo aField = aType.GetField(aName, SAVE_FIELDS);
                if (aField != null && !aField.IsInitOnly && GetFieldKind(aField.FieldType) == aKind)
                {
                    if (aKind == FIELD_INTS && aField.GetValue(theObject) is int[] anOld)
                    {
                        Array.Copy((int[])aValue, anOld, Math.Min(anOld.Length, ((int[])aValue).Length));
                    }
                    else
                    {
                        aField.SetValue(theObject, aValue);
                    }
                }
            }
        }

        private static int GetFieldKind(Type theType)
        {
            if (theType == typeof(int)) return FIELD_INT;
            if (theType == typeof(double)) return FIELD_DOUBLE;
            if (theType == typeof(bool)) return FIELD_BOOL;
            if (theType == typeof(char)) return FIELD_CHAR;
            if (theType == typeof(string)) return FIELD_STRING;
            if (theType == typeof(int[])) return FIELD_INTS;
            if (theType == typeof(Piece)) return FIELD_PIECE;
            return -1;
        }
    }
}

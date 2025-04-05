namespace Chess {
	// Thanks to https://web.archive.org/web/20071031100051/http://www.brucemo.com/compchess/programming/hashing.htm
	public class TranspositionTable {

		public const int lookupFailed = int.MinValue;

		public const int Exact = 0;
		public const int LowerBound = 1;
		public const int UpperBound = 2;

		public Entry[] entries;

		public readonly ulong count;
		public bool enabled = true;
		Board board;

		public TranspositionTable (Board board, int sizeMB) {
			this.board = board;
			
			int ttEntrySizeBytes = System.Runtime.InteropServices.Marshal.SizeOf<TranspositionTable.Entry>();
			int ttSize = sizeMB * 1024 * 1024;
			int numEntries = ttSize / ttEntrySizeBytes;

			count = (ulong)(numEntries);
			entries = new Entry[numEntries];
		}

		public void Clear () {
			for (int i = 0; i < entries.Length; i++) {
				entries[i] = new Entry ();
			}
		}

		public ulong Index {
			get {
				return board.ZobristKey % count;
			}
		}

		public Move GetStoredMove () {
			return entries[Index].move;
		}

		public int LookupEvaluation (int depth, int plyFromRoot, int alpha, int beta) {
			if (!enabled) {
				return lookupFailed;
			}
			Entry entry = entries[Index];

			if (entry.key == board.ZobristKey) {
				// Only use stored evaluation if it has been searched to at least the same depth as would be searched now
				if (entry.depth >= depth) {
					int correctedScore = CorrectRetrievedMateScore (entry.value, plyFromRoot);
					// We have stored the exact evaluation for this position, so return it
					if (entry.nodeType == Exact) {
						return correctedScore;
					}
					// We have stored the upper bound of the eval for this position. If it's less than alpha then we don't need to
					// search the moves in this position as they won't interest us; otherwise we will have to search to find the exact value
					if (entry.nodeType == UpperBound && correctedScore <= alpha) {
						return correctedScore;
					}
					// We have stored the lower bound of the eval for this position. Only return if it causes a beta cut-off.
					if (entry.nodeType == LowerBound && correctedScore >= beta) {
						return correctedScore;
					}
				}
			}
			return lookupFailed;
		}

		public void StoreEvaluation (int depth, int numPlySearched, int eval, int evalType, Move move) {
			if (!enabled) {
				return;
			}
			//ulong index = Index;
			//if (depth >= entries[Index].depth) {
			Entry entry = new Entry (board.ZobristKey, CorrectMateScoreForStorage (eval, numPlySearched), (byte) depth, (byte) evalType, move);
			entries[Index] = entry;
			//}
		}

		int CorrectMateScoreForStorage (int score, int numPlySearched) {
			if (Search.IsMateScore (score)) {
				int sign = System.Math.Sign (score);
				return (score * sign + numPlySearched) * sign;
			}
			return score;
		}

		int CorrectRetrievedMateScore (int score, int numPlySearched) {
			if (Search.IsMateScore (score)) {
				int sign = System.Math.Sign (score);
				return (score * sign - numPlySearched) * sign;
			}
			return score;
		}

		public struct Entry {

			public readonly ulong key;
			public readonly int value;
			public readonly Move move;
			public readonly byte depth;
			public readonly byte nodeType;

			//	public readonly byte gamePly;

			public Entry (ulong key, int value, byte depth, byte nodeType, Move move) {
				this.key = key;
				this.value = value;
				this.depth = depth; // depth is how many ply were searched ahead from this position
				this.nodeType = nodeType;
				this.move = move;
			}

			public static int GetSize () {
				return System.Runtime.InteropServices.Marshal.SizeOf<Entry> ();
			}
		}
	}
}
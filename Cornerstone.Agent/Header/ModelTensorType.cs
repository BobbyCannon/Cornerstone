namespace Cornerstone.Agent.Header;

public enum ModelTensorType : uint
{
	GgmlTypeF32 = 0,
	GgmlTypeF16 = 1,
	GgmlTypeQ40 = 2,
	GgmlTypeQ41 = 3,

	// GGML_TYPE_Q4_2 = 4, support has been removed
	// GGML_TYPE_Q4_3 = 5, support has been removed
	GgmlTypeQ50 = 6,
	GgmlTypeQ51 = 7,
	GgmlTypeQ80 = 8,
	GgmlTypeQ81 = 9,
	GgmlTypeQ2K = 10,
	GgmlTypeQ3K = 11,
	GgmlTypeQ4K = 12,
	GgmlTypeQ5K = 13,
	GgmlTypeQ6K = 14,
	GgmlTypeQ8K = 15,
	GgmlTypeIq2Xxs = 16,
	GgmlTypeIq2Xs = 17,
	GgmlTypeIq3Xxs = 18,
	GgmlTypeIq1S = 19,
	GgmlTypeIq4Nl = 20,
	GgmlTypeIq3S = 21,
	GgmlTypeIq2S = 22,
	GgmlTypeIq4Xs = 23,
	GgmlTypeI8 = 24,
	GgmlTypeI16 = 25,
	GgmlTypeI32 = 26,
	GgmlTypeI64 = 27,
	GgmlTypeF64 = 28,
	GgmlTypeIq1M = 29,
	GgmlTypeCount
}
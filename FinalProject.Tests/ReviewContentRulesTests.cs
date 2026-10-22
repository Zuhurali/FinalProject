using FinalProject.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class ReviewContentRulesTests
{
    // ── Acceptance cases ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("a")]                                                        // single char
    [InlineData("Good part.")]                                               // normal short sentence
    [InlineData("Excellent quality. Arrived quickly and fits perfectly.")]   // longer prose
    [InlineData("ser ser ser ser")]                                          // 4 repeats — one below threshold
    [InlineData("nice nice nice nice")]                                      // exactly 4 consecutive — below threshold
    [InlineData("The brake pads are good good good good but delivery was slow.")] // 4 repeats embedded in sentence
    public void Normalize_accepts_valid_content(string content)
    {
        var error = ReviewContentRules.Normalize(content, out var normalized);
        Assert.Null(error);
        Assert.Equal(content.Trim(), normalized);
    }

    // ── Spam rejection ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("ser ser ser ser ser ser ser ser ser ser")]   // req spec example — 10 repeats
    [InlineData("spam spam spam spam spam")]                   // exactly 5 consecutive — at threshold
    [InlineData("word word word word word word")]              // 6 repeats
    [InlineData("SER SER SER SER SER")]                       // upper-case — case-insensitive
    [InlineData("Ser ser SER Ser ser")]                        // mixed case — still same token 5 ×
    [InlineData("x  x  x  x  x")]                             // extra spaces between — still 5 tokens
    public void Normalize_rejects_repeated_token_spam(string content)
    {
        var error = ReviewContentRules.Normalize(content, out _);
        Assert.NotNull(error);
        Assert.Contains("spam", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── Min-length / empty ────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Normalize_rejects_empty_or_whitespace(string? content)
    {
        var error = ReviewContentRules.Normalize(content, out _);
        Assert.NotNull(error);
    }

    // ── Max-length ────────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_rejects_content_over_max_length()
    {
        var content = new string('a', ReviewContentRules.MaxLength + 1);
        var error = ReviewContentRules.Normalize(content, out _);
        Assert.NotNull(error);
    }

    [Fact]
    public void Normalize_accepts_content_at_exact_max_length()
    {
        var content = new string('a', ReviewContentRules.MaxLength);
        var error = ReviewContentRules.Normalize(content, out _);
        Assert.Null(error);
    }

    // ── Internal helper unit-tests ────────────────────────────────────────────

    [Fact]
    public void CheckForRepeatedTokenSpam_returns_null_for_single_word()
    {
        Assert.Null(ReviewContentRules.CheckForRepeatedTokenSpam("hello"));
    }

    [Fact]
    public void CheckForRepeatedTokenSpam_returns_null_when_fewer_tokens_than_threshold()
    {
        // 4 tokens — cannot possibly reach MaxConsecutiveRepeats (5)
        var error = ReviewContentRules.CheckForRepeatedTokenSpam("a a a a");
        Assert.Null(error);
    }

    [Fact]
    public void CheckForRepeatedTokenSpam_returns_error_at_exact_threshold()
    {
        var tokens = string.Join(" ", Enumerable.Repeat("word", ReviewContentRules.MaxConsecutiveRepeats));
        var error = ReviewContentRules.CheckForRepeatedTokenSpam(tokens);
        Assert.NotNull(error);
    }

    [Fact]
    public void CheckForRepeatedTokenSpam_is_case_insensitive()
    {
        var error = ReviewContentRules.CheckForRepeatedTokenSpam("Word WORD word Word word");
        Assert.NotNull(error);
    }
}

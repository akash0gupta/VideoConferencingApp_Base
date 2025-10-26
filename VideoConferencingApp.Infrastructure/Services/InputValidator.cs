using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Services
{
    public class InputValidator : IInputValidator
    {
        private readonly ILogger<InputValidator> _logger;

        // Regular expressions for validation
        private static readonly Regex UserIdRegex = new(@"^[a-zA-Z0-9_-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_\s-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex IceCandidateRegex = new(@"^candidate:", RegexOptions.Compiled);

        public InputValidator(ILogger<InputValidator> logger)
        {
            _logger = logger;
        }

        public bool IsValidUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Invalid user ID: empty or whitespace");
                return false;
            }

            if (!UserIdRegex.IsMatch(userId))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                return false;
            }

            return true;
        }

        public bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Invalid username: empty or whitespace");
                return false;
            }

            if (!UsernameRegex.IsMatch(username))
            {
                _logger.LogWarning("Invalid username format: {Username}", username);
                return false;
            }

            return true;
        }

        public bool IsValidSdp(string sdp)
        {
            if (string.IsNullOrWhiteSpace(sdp))
            {
                _logger.LogWarning("Invalid SDP: empty or whitespace");
                return false;
            }

            // Basic SDP validation
            if (!sdp.Contains("v=0") || sdp.Length > 100000) // Max 100KB
            {
                _logger.LogWarning("Invalid SDP format or size");
                return false;
            }

            return true;
        }

        public bool IsValidIceCandidate(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                _logger.LogWarning("Invalid ICE candidate: empty or whitespace");
                return false;
            }

            if (!IceCandidateRegex.IsMatch(candidate) || candidate.Length > 1000)
            {
                _logger.LogWarning("Invalid ICE candidate format");
                return false;
            }

            return true;
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove potentially dangerous characters
            return Regex.Replace(input, @"[<>""']", string.Empty);
        }
    }

}

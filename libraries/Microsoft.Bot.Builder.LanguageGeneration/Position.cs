﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.LanguageGeneration
{
    /// <summary>
    /// Represents a line and character position, such as
    /// the position of the cursor.
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        /// <param name="line">The line number of the current position.</param>
        /// <param name="character">The character number of the current line.</param>
        public Position(int line, int character)
        {
            Line = line;
            Character = character;
        }

        /// <summary>
        /// Gets or sets the zero-based line value.
        /// </summary>
        /// <value>
        /// The zero-based line value.
        /// </value>
        public int Line { get; set; }

        /// <summary>
        /// Gets or sets the zero-based character value.
        /// </summary>
        /// <value>
        /// The zero-based character value.
        /// </value>
        public int Character { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string value.</returns>
        public override string ToString() => $"line {Line}:{Character}";
    }
}

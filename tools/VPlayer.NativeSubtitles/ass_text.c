/* SPDX-License-Identifier: LGPL-2.1-or-later */
#include "ass_text.h"
#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

static bool is_digit(unsigned char value)
{
    return value >= '0' && value <= '9';
}

static void drawing_mode(const unsigned char *tag, const unsigned char *end, bool *drawing)
{
    for (; tag < end; tag++)
    {
        if (*tag != '\\' || end - tag < 2)
            continue;
        if (tag[1] == 'r')
            *drawing = false;
        if (tag[1] != 'p' || end - tag < 3 || !is_digit(tag[2]))
            continue;
        *drawing = false;
        for (tag += 2; tag < end && is_digit(*tag); tag++)
            *drawing |= *tag != '0';
        tag--;
    }
}

char *vplayer_ass_text(const unsigned char *packet, size_t length)
{
    if (!packet || length == SIZE_MAX)
        return NULL;
    const unsigned char *end = memchr(packet, 0, length);
    if (!end)
        end = packet + length;

    // VLC delivers Matroska-style ASS events: eight metadata fields, then text.
    const unsigned char *text = packet;
    for (unsigned field = 0; field < 8; field++)
    {
        text = memchr(text, ',', end - text);
        if (!text)
            return NULL;
        text++;
    }

    char *result = malloc((size_t)(end - text) + 1);
    if (!result)
        return NULL;
    char *out = result;
    bool drawing = false, may_have_tags = true;
    while (text < end)
    {
        unsigned char ch = *text++;
        if (ch == '{' && may_have_tags)
        {
            const unsigned char *close = memchr(text, '}', end - text);
            if (close)
            {
                drawing_mode(text, close, &drawing);
                text = close + 1;
                continue;
            }
            may_have_tags = false;
        }
        if (drawing)
            continue;
        if (ch == '\\' && text < end)
        {
            switch (*text)
            {
                case 'N': ch = '\n'; text++; break;
                case 'n': ch = ' '; text++; break;
                case 'h': *out++ = (char)0xc2; ch = 0xa0; text++; break;
                case '{': case '}': ch = *text++; break;
            }
        }
        *out++ = (char)ch;
    }
    *out = '\0';
    if (out == result)
    {
        free(result);
        return NULL;
    }
    return result;
}

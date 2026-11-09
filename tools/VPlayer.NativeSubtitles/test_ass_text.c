/* SPDX-License-Identifier: LGPL-2.1-or-later */
#include "ass_text.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int check(const char *packet, const char *expected)
{
    char *actual = vplayer_ass_text((const unsigned char *)packet, strlen(packet));
    int success = expected ? actual && strcmp(actual, expected) == 0 : !actual;
    if (!success) fprintf(stderr, "Unexpected text: %s\n", actual ? actual : "(none)");
    free(actual);
    return success;
}

int main(void)
{
    const int success =
        check("0,0,Style,,0,0,0,,{\\fnFancy\\fs90\\c&HFFFFFF&}hello, world\\Nnext\\hline",
            "hello, world\nnext\xc2\xa0line") &&
        check("0,0,Style,,0,0,0,,{\\p1}m 0 0 l 5 5{\\p0}{\\pos(1,2)}text", "text") &&
        check("0,0,Style,,0,0,0,,{\\p2}m 0 0{\\rNormal}label", "label") &&
        check("0,0,Style,,0,0,0,,\\{literal\\} {unclosed", "{literal} {unclosed") &&
        check("0,0,Style,,0,0,0,,{\\p1}m 0 0 l 3 4", NULL) &&
        check("malformed packet", NULL);
    if (success) puts("Six ASS packet tests passed.");
    return success ? 0 : 1;
}

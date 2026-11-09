/* SPDX-License-Identifier: LGPL-2.1-or-later */
#ifndef VPLAYER_ASS_TEXT_H
#define VPLAYER_ASS_TEXT_H

#include <stddef.h>

/* Decode an ASS packet's dialogue as UTF-8 text. The caller owns the result. */
char *vplayer_ass_text(const unsigned char *packet, size_t length);

#endif

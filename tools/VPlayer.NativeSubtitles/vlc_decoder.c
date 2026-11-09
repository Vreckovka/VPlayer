/* SPDX-License-Identifier: LGPL-2.1-or-later */
#include <vlc_common.h>
#include <vlc_plugin.h>
#include <vlc_codec.h>
#include <vlc_charset.h>
#include "substext.h"
#include "ass_text.h"

static int Open(vlc_object_t *object);

vlc_module_begin()
    set_shortname("VPlayer subtitles")
    set_description("ASS subtitles using VPlayer's text appearance")
    set_capability("spu decoder", 150)
    set_category(CAT_INPUT)
    set_subcategory(SUBCAT_INPUT_SCODEC)
    set_callbacks(Open, NULL)
vlc_module_end()

static int Decode(decoder_t *decoder, block_t *block)
{
    if (!block)
        return VLCDEC_SUCCESS;
    if (block->i_pts != VLC_TICK_INVALID && !(block->i_flags & BLOCK_FLAG_CORRUPTED))
    {
        char *text = vplayer_ass_text(block->p_buffer, block->i_buffer);
        if (text)
        {
            EnsureUTF8(text);
            text_segment_t *segment = text_segment_New(text);
            free(text);
            if (segment)
            {
                subpicture_t *sub = decoder_NewSubpictureText(decoder);
                if (sub)
                {
                    vlc_tick_t duration = block->i_length;
                    if (duration < 0 || block->i_pts > INT64_MAX - duration)
                        duration = 0;
                    sub->i_start = block->i_pts;
                    sub->i_stop = block->i_pts + duration;
                    sub->b_ephemer = duration == 0;
                    sub->b_absolute = false;
                    sub->updater.p_sys->region.align = SUBPICTURE_ALIGN_BOTTOM;
                    sub->updater.p_sys->region.p_segments = segment;
                    decoder_QueueSub(decoder, sub);
                }
                else
                    text_segment_Delete(segment);
            }
        }
    }
    block_Release(block);
    return VLCDEC_SUCCESS;
}

static int Open(vlc_object_t *object)
{
    decoder_t *decoder = (decoder_t *)object;
    if (decoder->fmt_in.i_codec != VLC_CODEC_SSA)
        return VLC_EGENERIC;
    decoder->fmt_out.i_codec = 0;
    decoder->pf_decode = Decode;
    return VLC_SUCCESS;
}
